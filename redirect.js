(function() {
    let location = window.location;
    if (location.search) {
        const segments = 1;
        for (let i = 0, n = 0, length = location.pathname.length; i < length; ++i) {
            if (location.pathname[i] === '/' && ++n === segments + 1) {
                if (i !== length - 1)
                    return;
            }
        }
        let parameters = new URLSearchParams(location.search.slice(1));
        let path = parameters.get('page');
        if (path) {
            let url = location.pathname + decodeURIComponent(path);
            let query = parameters.get('query');
            if (query) {
                url += '?' + decodeURIComponent(query);
            }
            if (location.hash) {
                url += location.hash;
            }
            window.history.replaceState(null, null, url);
        }
    }
}())
