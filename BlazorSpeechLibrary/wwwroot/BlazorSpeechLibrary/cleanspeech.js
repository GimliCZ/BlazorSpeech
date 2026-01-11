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
            const handler = () => {
                const loaded = this.synth.getVoices();
                if (loaded && loaded.length > 0) {
                    this.voices = loaded;
                    this.synth.removeEventListener("voiceschanged", handler);
                    resolve();
                }
            };

            this.synth.addEventListener("voiceschanged", handler);
        });
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

        // No events wired – JS owns lifecycle fully
        this.synth.speak(u);
    }

    stop() {
        this._throwIfDisposed();
        this.synth.cancel();
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
        this.synth.cancel();
        this.voices = null;
        this.voiceReady = null;
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
 * ============================ */

export function speak(options) {
    getController().speak(options);
}

export function stop() {
    getController().stop();
}

export function isSpeaking() {
    return getController().isSpeaking();
}

export async function getVoices() {
    return await getController().getVoices();
}

export function dispose() {
    if (controller) {
        controller.dispose();
        controller = null;
    }
}
