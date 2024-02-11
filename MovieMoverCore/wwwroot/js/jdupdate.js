function awaitUpdateEnd() {
    $.ajax({
        url: '/api/JDUpdate/JDState',
        type: 'GET'
    })
        .done(function (response) {
            if (response === "Ready") {
                $('#jd_updating').slideUp();
                $('#jd_update_success').slideDown();
            } else {
                setTimeout(awaitUpdateEnd, 5000);
            }
        })
        .fail(function (xhr, textStatus, errorThrown) {
            $('#jd_error').innerText = "Failed getting JD State: " + xhr.responseText + " - Try reload the page manually";
            $('#jd_error').slideDown();
        });
}

function invokeJDUpdate() {
    $.ajax({
        url: '/api/JDUpdate/InvokeUpdate',
        method: 'POST'
    })
        .done(function (response) {
            $('#jd_update_info').slideUp();
            $('#jd_updating').slideDown();
            setTimeout(awaitUpdateEnd, 20000);
        })
        .fail(function (xhr, textStatus, errorThrown) {
            $('#jd_error').innerText = "Failed invoking JD Update: " + xhr.responseText;
            $('#jd_error').slideDown();
        });
}

function checkUpdateAvailable() {
    $.ajax({
        url: '/api/JDUpdate/IsUpdateAvailable',
        type: 'GET'
    })
        .done(function (response) {
            is_avail = JSON.parse(response);
            if (is_avail === true) {
                $('#jd_update_info').slideDown();
            } else {
                console.log("No JD Update available");
            }
        })
        .fail(function (xhr, textStatus, errorThrown) {
            $('#jd_error').innerText = "Failed getting JD Update status: " + xhr.responseText;
            $('#jd_error').slideDown();
        });
}

function checkJDState() {
    $.ajax({
        url: '/api/JDUpdate/JDState',
        type: 'GET'
    })
        .done(function (state) {
            if (state !== "Ready") {
                $('#jd_warning').html("There seems to be a problem with JDownloader. State should be \"Ready\" but is \"" + state + "\".");
                $('#jd_warning').slideDown();
                setTimeout(checkJDState, 5000);
            } else {
                console.log("JD is Ready :)");
                $('#jd_warning').slideUp();
                checkUpdateAvailable();
            }
        })
        .fail(function (xhr, textStatus, errorThrown) {
            $('#jd_error').innerText = "Failed getting JDState: " + xhr.responseText;
            $('#jd_error').slideDown();
        });
}

function invokeJDUpdateSearch() {
    $.ajax({
        url: '/api/JDUpdate/InvokeUpdateCheck',
        method: 'POST'
    })
        .done(function (response) {
            $('#jd_success').html("Successfully invoked update-check.");
            $('#jd_success').slideDown();
            setTimeout(checkUpdateAvailable, 5000);
            setTimeout(() => $('#jd_success').slideUp(), 5000);
        })
        .fail(function (xhr, textStatus, errorThrown) {
            $('#jd_error').innerText = "Failed invoking JD Update search: " + xhr.responseText;
            $('#jd_error').slideDown();
        });
}

checkJDState();