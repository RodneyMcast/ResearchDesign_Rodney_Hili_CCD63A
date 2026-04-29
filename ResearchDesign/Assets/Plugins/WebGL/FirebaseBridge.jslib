mergeInto(LibraryManager.library, {
    SavePlayerData: function (jsonPtr) {
        var json = UTF8ToString(jsonPtr);

        if (window.FirebaseBridge && typeof window.FirebaseBridge.savePlayerData === 'function') {
            window.FirebaseBridge.savePlayerData(json);
            return;
        }

        console.error('[FirebaseBridge.jslib] window.FirebaseBridge.savePlayerData is not ready.');
    },

    GetPlayerData: function (idPtr) {
        var id = UTF8ToString(idPtr);

        if (window.FirebaseBridge && typeof window.FirebaseBridge.getPlayerData === 'function') {
            window.FirebaseBridge.getPlayerData(id);
            return;
        }

        console.error('[FirebaseBridge.jslib] window.FirebaseBridge.getPlayerData is not ready.');
    },

    SubmitLevelAttempt: function (jsonPtr) {
        var json = UTF8ToString(jsonPtr);

        if (window.FirebaseBridge && typeof window.FirebaseBridge.submitLevelAttempt === 'function') {
            window.FirebaseBridge.submitLevelAttempt(json);
            return;
        }

        console.error('[FirebaseBridge.jslib] window.FirebaseBridge.submitLevelAttempt is not ready.');
    }
});
