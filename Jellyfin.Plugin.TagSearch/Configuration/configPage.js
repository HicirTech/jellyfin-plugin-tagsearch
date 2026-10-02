const TagSearchConfig = {
    pluginUniqueId: '3191e3af-6b61-4e2f-a363-76da407df83c'
};

export default function (view) {
    view.addEventListener('viewshow', function () {
        const target = view.querySelector('#TagSearchLoadedVersion');
        ApiClient.getJSON(ApiClient.getUrl('Plugins')).then(function (plugins) {
            const self = plugins.find(function (plugin) {
                return plugin.Id.replaceAll('-', '') === TagSearchConfig.pluginUniqueId.replaceAll('-', '');
            });
            target.textContent = self
                ? 'Loaded version ' + self.Version + ', status ' + self.Status + '.'
                : 'The server did not list this plugin.';
        }, function () {
            target.textContent = 'Could not read the plugin list.';
        });
    });
}
