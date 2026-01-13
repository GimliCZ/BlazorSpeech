// clean-speech.js
// ES module – loaded via JSImport / dynamic import

let controller = null;

/**
 * Internal controller – never exposed directly
 */
class SpeechController {
    constructor() {
        /** @type {SpeechSynthesis} */
        this.synth = window.speechSynthesis;

        /** @type {SpeechSynthesisVoice[] | null} */
        this.voices = null;

        /** @type {((ev: Event) => void) | null} */
        this._voicesChangedHandler = null;

        /** @type {Promise<void> | null} */
        this.voiceReady = null;

        /** @type {((isSpeaking: boolean) => void)[]} - State change callbacks */
        this._stateChangeCallbacks = [];

        /** @type {SpeechSynthesisUtterance | null} - Current utterance reference */
        this._currentUtterance = null;
        
        /** @type {boolean} */
        this.disposed = false;

        this._init();
    }

    _init() {
        // Voices may already be available
        const existing = this.synth.getVoices();
        if (existing && existing.length > 0) {
            this.voices = existing;
            this.voiceReady = Promise.resolve();
            return;
        }

        // Otherwise wait exactly once
        this.voiceReady = new Promise(resolve => {
            this._voicesChangedHandler = () => {
                const loaded = this.synth.getVoices();
                if (loaded && loaded.length > 0) {
                    this.voices = loaded;
                    this._removeVoicesChangedListener();
                    resolve();
                }
            };

            this.synth.addEventListener("voiceschanged", this._voicesChangedHandler);
        });
    }

    /**
     * Centralized listener removal
     * @private
     */
    _removeVoicesChangedListener() {
        if (this._voicesChangedHandler) {
            this.synth.removeEventListener("voiceschanged", this._voicesChangedHandler);
            this._voicesChangedHandler = null;
        }
    }

    async ensureVoices() {
        if (this.voices && this.voices.length > 0)
            return;

        if (this.voiceReady)
            await this.voiceReady;
    }

    speak(opts) {
        this._throwIfDisposed();

        if (!opts || !opts.text)
            return;

        // Cancel previous utterances – deterministic behavior
        this.synth.cancel();
        this._cleanupCurrentUtterance();

        const u = new SpeechSynthesisUtterance(opts.text);

        if (opts.lang)
            u.lang = opts.lang;

        if (typeof opts.rate === "number")
            u.rate = opts.rate;

        if (typeof opts.pitch === "number")
            u.pitch = opts.pitch;

        if (typeof opts.volume === "number")
            u.volume = opts.volume;

        if (opts.voice) {
            const v = this._findVoice(opts.voice, opts.lang);
            if (v)
                u.voice = v;
        }

        u.onstart = () => this._notifySpeakingState(true);
        u.onend = () => {
            this._notifySpeakingState(false);
            this._cleanupCurrentUtterance();
        };
        u.onerror = () => {
            this._notifySpeakingState(false);
            this._cleanupCurrentUtterance();
        };

        this._currentUtterance = u;

        // No events wired – JS owns lifecycle fully
        this.synth.speak(u);
    }

    cancel() {
        this._throwIfDisposed();
        this.synth.cancel();
        this._cleanupCurrentUtterance();
        this._notifySpeakingState(false);
    }

    pause() {
        this._throwIfDisposed();
        this.synth.pause();
        this._notifySpeakingState(false);
    }

    resume() {
        this._throwIfDisposed();
        this.synth.resume();
        this._notifySpeakingState(true);
    }

    getPaused() {
        if (this.disposed)
            return false;
        return this.synth.paused;
    }

    getPending() {
        if (this.disposed)
            return false;
        return this.synth.pending;
    }

    getSpeaking() {
        if (this.disposed)
            return false;
        return this.synth.speaking;
    }

    isSpeaking() {
        if (this.disposed)
            return false;
        return this.synth.speaking || this.synth.pending;
    }

    async getVoices() {
        this._throwIfDisposed();
        await this.ensureVoices();

        if (!this.voices)
            return [];

        // Return pure DTOs – no browser objects cross boundary
        return this.voices.map(v => ({
            name: v.name,
            lang: v.lang,
            voiceUri: v.voiceURI,
            isDefault: v.default,
            isLocalService: v.localService
        }));
    }

    dispose() {
        if (this.disposed)
            return;

        this.disposed = true;
        
        // Clean up current utterance
        this._cleanupCurrentUtterance();
        
        // Critical: Remove event listener before clearing state
        this._removeVoicesChangedListener();

        // Cancel any ongoing speech
        this.synth.cancel();

        // Clear all references to prevent memory leaks
        this.voices = null;
        this.voiceReady = null;
        this.synth = null;
    }

    /**
     * Clean up current utterance event handlers
     * @private
     */
    _cleanupCurrentUtterance() {
        if (this._currentUtterance) {
            this._currentUtterance.onstart = null;
            this._currentUtterance.onend = null;
            this._currentUtterance.onerror = null;
            this._currentUtterance = null;
        }
    }

    /**
     * Notify all callbacks of speaking state change
     * @param {boolean} isSpeaking
     * @private
     */
    _notifySpeakingState(isSpeaking) {
        if (this.disposed)
            return;

        // Notify all registered callbacks
        for (const callback of this._stateChangeCallbacks) {
            try {
                callback(isSpeaking);
            } catch (err) {
                console.error("Error in speaking state callback:", err);
            }
        }
    }

    /**
     * Register callback for speaking state changes
     * @param {(isSpeaking: boolean) => void} callback
     * @returns {() => void} Unsubscribe function
     */
    onSpeakingStateChanged(callback) {
        this._throwIfDisposed();

        if (typeof callback !== "function")
            throw new Error("Callback must be a function");

        this._stateChangeCallbacks.push(callback);

        // Return unsubscribe function
        return () => {
            const index = this._stateChangeCallbacks.indexOf(callback);
            if (index !== -1) {
                this._stateChangeCallbacks.splice(index, 1);
            }
        };
    }

    _findVoice(name, lang) {
        if (!this.voices)
            return null;

        // Exact match first
        let v = this.voices.find(x => x.name === name);
        if (v)
            return v;

        // Fallback: name + language
        if (lang)
            return this.voices.find(x => x.lang === lang) ?? null;

        return null;
    }

    _throwIfDisposed() {
        if (this.disposed)
            throw new Error("SpeechController disposed");
    }
}

/**
 * Lazy singleton initializer
 */
function getController() {
    if (!controller)
        controller = new SpeechController();

    return controller;
}

/* ============================
 * Public module API (Blazor)
 * Complete SpeechSynthesis interface exposure
 * ============================ */

/**
 * Speak text with options
 * @param {Object} options - Speech options
 * @param {string} options.text - Text to speak
 * @param {string} [options.lang] - Language code
 * @param {number} [options.rate] - Speech rate (0.1-10)
 * @param {number} [options.pitch] - Speech pitch (0-2)
 * @param {number} [options.volume] - Speech volume (0-1)
 * @param {string} [options.voice] - Voice name
 */
export function speak(options) {
    getController().speak(options);
}

/**
 * Cancel all queued and current speech
 */
export function cancel() {
    getController().cancel();
}

/**
 * Pause speech synthesis
 */
export function pause() {
    getController().pause();
}

/**
 * Resume paused speech synthesis
 */
export function resume() {
    getController().resume();
}

/**
 * Check if speech is currently being spoken or pending
 * @returns {boolean}
 */
export function isSpeaking() {
    return getController().isSpeaking();
}

/**
 * Get paused state
 * @returns {boolean} - True if speech is paused
 */
export function getPaused() {
    return getController().getPaused();
}

/**
 * Get pending state
 * @returns {boolean} - True if utterances are in queue
 */
export function getPending() {
    return getController().getPending();
}

/**
 * Get speaking state
 * @returns {boolean} - True if currently speaking
 */
export function getSpeaking() {
    return getController().getSpeaking();
}

/**
 * Get available voices
 * @returns {Promise<Array<{name: string, lang: string, voiceUri: string, isDefault: boolean, isLocalService: boolean}>>}
 */
export async function getVoices() {
    return await getController().getVoices();
}

/**
 * Dispose controller and clean up resources
 */
export function dispose() {
    if (controller) {
        controller.dispose();
        controller = null;
    }
}

/**
 * Subscribe to speaking state changes
 * @param {DotNetObjectReference} dotNetHelper - .NET object reference
 * @param {string} methodName - Method name to invoke on state change
 * @returns {Promise<{callback: Function}>} Object containing the callback for unsubscribe
 *
 * @example
 * // From C#:
 * var handle = await module.InvokeAsync<IJSObjectReference>(
 *     "onSpeakingStateChanged",
 *     DotNetObjectReference.Create(this),
 *     nameof(OnSpeakingStateChanged));
 */
export async function onSpeakingStateChanged(dotNetHelper, methodName) {
    const controller = getController();

    // Create callback that invokes the C# method
    const callback = async (isSpeaking) => {
        try {
            await dotNetHelper.invokeMethodAsync(methodName, isSpeaking);
        } catch (err) {
            console.error("Error invoking .NET callback:", err);
        }
    };

    // Subscribe and get unsubscribe function
    const unsubscribe = controller.onSpeakingStateChanged(callback);

    // Return object with callback reference for cleanup
    // C# can store this and call unsubscribeFromSpeakingState(callback) later
    return { callback, unsubscribe };
}

/**
 * Unsubscribe from speaking state changes
 * @param {{callback: Function}} handle - Handle returned from onSpeakingStateChanged
 */
export function unsubscribeFromSpeakingState(handle) {
    if (handle && typeof handle.unsubscribe === "function") {
        handle.unsubscribe();
    }
}