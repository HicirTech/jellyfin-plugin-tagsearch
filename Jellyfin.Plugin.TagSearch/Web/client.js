// Replaces the search page's random suggestions with the user's saved and recent searches.
// A search counts as used, and joins the recent list, when one of its results is opened.
(function () {
    'use strict';

    if (window.TagSearchClientLoaded) {
        return;
    }
    window.TagSearchClientLoaded = true;

    const PANEL_CLASS = 'tagSearchPanel';
    const isChinese = (document.documentElement.lang || navigator.language || '').toLowerCase().indexOf('zh') === 0;
    const text = isChinese ? {
        saved: '已保存',
        recent: '最近搜索',
        noSaved: '点星标把常用的搜索保存在这里。',
        noRecent: '搜索后点开一部影片，这次搜索就会出现在这里。',
        save: '保存',
        unsave: '取消保存',
        remove: '删除',
        failed: '无法读取搜索记录。'
    } : {
        saved: 'Saved searches',
        recent: 'Recent searches',
        noSaved: 'Star a search to keep it here.',
        noRecent: 'Open a result of a search and the search appears here.',
        save: 'Save',
        unsave: 'Remove from saved',
        remove: 'Remove',
        failed: 'Could not load your searches.'
    };

    function call(method, path, query) {
        const client = window.ApiClient;
        const url = client.getUrl('TagSearch/' + path, query === undefined ? undefined : { query: query });
        return method === 'GET'
            ? client.getJSON(url)
            : client.ajax({ type: method, url: url, dataType: 'json' });
    }

    function currentQuery() {
        const hash = window.location.hash;
        const at = hash.indexOf('?');
        if (hash.indexOf('#/search') !== 0 || at < 0) {
            return '';
        }
        return (new URLSearchParams(hash.slice(at + 1)).get('query') || '').trim();
    }

    function iconButton(icon, title, onClick) {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'paper-icon-button-light';
        button.title = title;
        button.setAttribute('aria-label', title);
        const glyph = document.createElement('span');
        glyph.className = 'material-icons ' + icon;
        glyph.setAttribute('aria-hidden', 'true');
        button.appendChild(glyph);
        button.addEventListener('click', function (event) {
            event.preventDefault();
            onClick();
        });
        return button;
    }

    function section(panel, title, queries, emptyText, buttons) {
        const heading = document.createElement('h2');
        heading.className = 'sectionTitle';
        heading.textContent = title;
        panel.appendChild(heading);

        if (!queries.length) {
            const empty = document.createElement('p');
            empty.className = 'fieldDescription';
            empty.textContent = emptyText;
            panel.appendChild(empty);
            return;
        }

        queries.forEach(function (query) {
            const row = document.createElement('div');
            row.className = 'tagSearchRow';
            const link = document.createElement('a');
            link.className = 'button-link';
            link.href = '#/search?query=' + encodeURIComponent(query);
            link.textContent = query;
            link.addEventListener('click', function () {
                call('POST', 'Searches/Recent', query);
            });
            row.appendChild(link);
            buttons(query).forEach(function (button) {
                row.appendChild(button);
            });
            panel.appendChild(row);
        });
    }

    function render(panel, lists) {
        const saved = lists.Saved || [];
        const recent = (lists.Recent || []).filter(function (query) {
            return saved.indexOf(query) < 0;
        });
        const refresh = function (request) {
            request.then(function (updated) {
                render(panel, updated);
            });
        };

        panel.replaceChildren();
        section(panel, text.saved, saved, text.noSaved, function (query) {
            return [iconButton('star', text.unsave, function () {
                refresh(call('DELETE', 'Searches/Saved', query));
            })];
        });
        section(panel, text.recent, recent, text.noRecent, function (query) {
            return [
                iconButton('star_border', text.save, function () {
                    refresh(call('POST', 'Searches/Saved', query));
                }),
                iconButton('close', text.remove, function () {
                    refresh(call('DELETE', 'Searches/Recent', query));
                })
            ];
        });
    }

    // The suggestions block exists only while the search box is empty, and the panel lives inside
    // it, so the panel disappears with it and is added again when the box is cleared.
    function inject() {
        const host = document.querySelector('#searchPage .searchSuggestions');
        if (!host || host.querySelector('.' + PANEL_CLASS) || !window.ApiClient) {
            return;
        }
        const panel = document.createElement('div');
        panel.className = PANEL_CLASS;
        host.appendChild(panel);
        call('GET', 'Searches').then(function (lists) {
            render(panel, lists);
        }, function () {
            panel.textContent = text.failed;
        });
    }

    const style = document.createElement('style');
    style.textContent = [
        '#searchPage .searchSuggestions > :not(.' + PANEL_CLASS + ') { display: none !important; }',
        '.' + PANEL_CLASS + ' { max-width: 40em; margin: 0 auto; padding: 0 1em; text-align: left; }',
        '.' + PANEL_CLASS + ' .tagSearchRow { display: flex; align-items: center; }',
        '.' + PANEL_CLASS + ' .tagSearchRow a { flex-grow: 1; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; padding: 0.5em 0; }'
    ].join('\n');
    document.head.appendChild(style);

    // A timer rather than an animation frame: frames pause while the tab or app is in the
    // background, and the panel must already be in place when it comes back.
    let scheduled = false;
    new MutationObserver(function () {
        if (!scheduled) {
            scheduled = true;
            window.setTimeout(function () {
                scheduled = false;
                inject();
            }, 50);
        }
    }).observe(document.body, { childList: true, subtree: true });
    inject();

    document.addEventListener('click', function (event) {
        const query = currentQuery();
        if (query && event.target instanceof Element && event.target.closest('#searchPage .card')) {
            call('POST', 'Searches/Recent', query);
        }
    }, true);
})();
