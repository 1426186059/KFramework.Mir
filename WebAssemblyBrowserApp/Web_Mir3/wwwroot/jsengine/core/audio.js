// jsengine/core/audio.js
// Web Audio 音效后端。对应 JSBind/BrowserAudio.cs（mir.initAudio / playSound / stopSound / stopAllSounds / setSoundVolume）。
import { audio, ensureAudio, fetchBytes, decodeWav } from '../shared.js';

// 浏览器自动播放策略：用户首次交互时恢复 AudioContext
function resumeAudio() {
    ensureAudio();
    window.removeEventListener('pointerdown', resumeAudio);
    window.removeEventListener('keydown', resumeAudio);
}
window.addEventListener('pointerdown', resumeAudio);
window.addEventListener('keydown', resumeAudio);

export const initAudio = () => { ensureAudio(); };

export const playSound = (url, volume, loop) => {
    try {
        const ctx = ensureAudio();
        if (!ctx) return 0;
        const data = fetchBytes(url);
        if (!data) return 0;
        const buf = decodeWav(data);
        if (!buf) return 0;
        const src = ctx.createBufferSource();
        src.buffer = buf;
        src.loop = !!loop;
        const gain = ctx.createGain();
        gain.gain.value = Math.max(0, Math.min(1, volume / 100));
        src.connect(gain).connect(ctx.destination);
        src.start(0);
        const id = audio.nextId++;
        audio.active.set(id, { src, gain });
        src.onended = () => audio.active.delete(id);
        return id;
    } catch (e) { return 0; }
};

export const stopSound = (id) => {
    const a = audio.active.get(id);
    if (a) { try { a.src.stop(); } catch (e) { } audio.active.delete(id); }
};

export const stopAllSounds = () => {
    for (const [, a] of audio.active) { try { a.src.stop(); } catch (e) { } }
    audio.active.clear();
};

export const setSoundVolume = (id, volume) => {
    const a = audio.active.get(id);
    if (a) a.gain.gain.value = Math.max(0, Math.min(1, volume / 100));
};
