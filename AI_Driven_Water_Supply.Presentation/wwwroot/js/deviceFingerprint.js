window.getDeviceFingerprint = async function () {
    try {
        if (typeof FingerprintJS === 'undefined') {
            return '';
        }
        const fp = await FingerprintJS.load();
        const result = await fp.get();
        return result.visitorId || '';
    } catch (e) {
        console.warn('getDeviceFingerprint', e);
        return '';
    }
};
