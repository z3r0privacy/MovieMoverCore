/*
$('#dateRelease').datepicker({
    format: "dd.mm.yyyy",
    weekStart: 1,
    todayHighlight: true
});
*/

function getDetailState(xhr) {
    state = "";
    if (xhr.status === 404) {
        state += "To-Do not found";
    } else if (xhr.status === 400) {
        err = JSON.parse(xhr.responseText);
        state += err.title;
        state += "<ul>";
        for (const [key, value] of Object.entries(err.errors)) {
            state += "<li>" + key + ": ";
            state += value.join(", ") + "</li>";
        }
        state += "</ul>";
    } else {
        state += xhr.status;
        if (xhr.responseText !== "") {
            state += " - " + xhr.responseText;
        }
    }
    return state;
}

function downloaded(idx) {
    $.ajax({
        url: '/api/Obtain/Obtained/'+idx,
        type: 'PUT'
    })
        .done(function (response) {
            location.reload()
        })
        .fail(function (xhr, textStatus, errorThrown) {
            state = "Set downloaded failed: " + getDetailState(xhr);            
            $('#err_error').html(state);
            $('#err_error').slideDown();
        });
}

function deleteObtain(idx) {
    $.ajax({
        url: '/api/Obtain/Delete/' + idx,
        type: 'DELETE'
    })
        .done(function (response) {
            location.reload()
        })
        .fail(function (xhr, textStatus, errorThrown) {
            state = "Deleting failed: " + getDetailState(xhr);
            $('#err_error').html(state);
            $('#err_error').slideDown();
        });
}

function fixDateFormat(date) {
    parts = date.split(".");
    if (parts.length !== 3) {
        return date;
    }
    return parts[2] + "-" + parts[1] + "-" + parts[0];
}

function addObtain() {
    $.ajax({
        url: '/api/Obtain/AddObtain',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({
            name: $("#txt_name")[0].value,
            releaseDate: fixDateFormat($("#dateRelease")[0].value)
        })
    })
        .done(function (response) {
            $('#ObtainsModal').modal('hide');
            location.reload();
        })
        .fail(function (xhr, textStatus, errorThrown) {
            state = "Loading data for updating failed: " + getDetailState(xhr);
            $('#obtainModalError').html(state);
            $('#obtainModalError').slideDown();
        });
}

function updateObtain() {
    $.ajax({
        url: '/api/Obtain/UpdateObtain',
        type: 'PUT',
        contentType: 'application/json',
        data: JSON.stringify({
            id: +$("#obtain_idx")[0].value,
            name: $("#txt_name")[0].value,
            releaseDate: fixDateFormat($("#dateRelease")[0].value)
        })
    })
        .done(function (response) {
            $('#ObtainsModal').modal('hide');
            location.reload();
        })
        .fail(function (xhr, textStatus, errorThrown) {
            state = "Loading data for updating failed: " + getDetailState(xhr);
            $('#obtainModalError').html(state);
            $('#obtainModalError').slideDown();
        });
}

function showModalInner(data) {
    $('#obtainModalError').hide();
    $('#dateRelease').datepicker({
        format: "dd.mm.yyyy",
        weekStart: 1,
        todayHighlight: true
    });
    if (data === null) {
        $("#obtainModelSubmit").html("Add");
        $("#obtainModelSubmit")[0].onclick = function () {
            addObtain();
        };
        $("#obtainModalLabel").html("Add To-Do");
        $("#obtain_idx")[0].value = "-1";
        $("#txt_name")[0].value = "";
        $("#dateRelease").datepicker('update', '');
    } else {
        $("#obtainModelSubmit").html("Update");
        $("#obtainModelSubmit")[0].onclick = function () {
            updateObtain();
        };
        $("#obtainModalLabel").html("Update To-Do");
        $("#obtain_idx")[0].value = data.id;
        $("#txt_name")[0].value = data.name;
        $("#dateRelease").datepicker('update', data.releaseDate);
    }
    $('#ObtainsModal').modal();
}

function showAddModal() {
    showModalInner(null);
}

function showEditModal(idx) {
    $.ajax({
        url: '/api/Obtain/' + idx,
        type: 'GET'
    })
        .done(function (response) {
            showModalInner(response);
        })
        .fail(function (xhr, textStatus, errorThrown) {
            state = "Loading data for updating failed: " + getDetailState(xhr);
            $('#err_error').html(state);
            $('#err_error').slideDown();
        });
}