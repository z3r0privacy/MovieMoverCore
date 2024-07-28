function reloadRenames() {
    $.ajax({
        url: '/api/Maintenance/ReloadRenamings',
        method: 'POST'
    }).done(function (response) {
        location.reload();
    });
}
