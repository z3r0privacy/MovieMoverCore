function dlVideo(sId) {
    $.ajax({
        url: '/episodeguide?handler=DownloadVideo',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(sId),
        headers: {
            RequestVerificationToken: document.getElementById('RequestVerificationToken').value
        }
    })
        .done(function (result) {
            alert(result);
        });
}


function dlSub(sId) {
    $.ajax({
        url: '/episodeguide?handler=DownloadSub',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(sId),
        headers: {
            RequestVerificationToken: document.getElementById('RequestVerificationToken').value
        }
    })
        .done(function (result) {
            alert(result);
        });
}