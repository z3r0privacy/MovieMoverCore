selectedCards = [];
function toggleSelection(cardId) {
    var io = selectedCards.indexOf(cardId);
    if (io !== -1) {
        delete selectedCards[io];
        document.getElementById(cardId).classList.remove("selectedCard");
    } else {
        selectedCards.push(cardId);
        document.getElementById(cardId).classList.add("selectedCard");
    }
}

function selectAll() {
    dlDiv = document.getElementById("downloadCards");
    dlCards = dlDiv.querySelectorAll(":scope > div");
    allSelected = true;
    for (var i = 0; i < dlCards.length && allSelected; i++) {
        var currentId = dlCards[i].firstElementChild.getAttribute("id");
        var io = selectedCards.indexOf(currentId);
        if (io === -1) {
            allSelected = false;
        }
    }

    var shouldSelect = !allSelected;
    for (var i = 0; i < dlCards.length; i++) {
        var currentId = dlCards[i].firstElementChild.getAttribute("id");
        var io = selectedCards.indexOf(currentId);
        if (io === -1 && shouldSelect) {
            toggleSelection(currentId);
        } else if (io !== -1 && !shouldSelect) {
            toggleSelection(currentId);
        }
    }
}

function getDownloadData() {
    $.ajax({
        url: '/Downloads?handler=Downloads',
        type: 'GET',
        contentType: 'application/json',
    })
        .done(function (result) {
            var copy = [...selectedCards];
            selectedCards.length = 0;
            document.getElementById("downloadCards").innerHTML = JSON.parse(result);
            for (var i = 0; i < copy.length; i++) {
                var el = document.getElementById(copy[i]);
                if (el) {
                    el.classList.add("selectedCard");
                    selectedCards.push(copy[i]);
                }
            }
        })
        .fail(function (xhr, textStatus, errorThrown) {
            console.log("Error refreshing download state: " + xhr.responseText);
        })
        .always(function () {
            setTimeout(getDownloadData, refreshInterval);
        });
}

function getDownloadControllerStatus() {
    $.ajax({
        url: '/api/Downloads/ControllerStatus',
        type: 'GET',
        contentType: 'application/json',
    })
        .done(function (state) {
            if (state.downloading === true) {
                document.getElementById("dlspeed").innerHTML = "Downloading: " + state.speed + " " + state.unit;
                document.getElementById("btnRestart").style.visibility = "visible";
            } else {
                document.getElementById("dlspeed").innerHTML = "";
                document.getElementById("btnRestart").style.visibility = "hidden";
            }
        })
        .fail(function (xhr, textStatus, errorThrown) {
            console.log("Error refreshing download controller (speed) state: " + xhr.responseText);
        })
        .always(function () {
            setTimeout(getDownloadControllerStatus, refreshInterval);
        });
}

var templatePendingPackage = `
<div id="{id}" class="col-md-4 my-2">
    <div class="card" style="width: 18rem;">
        <div class="card-body">
            <h6 class="card-subtitle mb-2 text-muted">{name} ({size} {unit})</h6>
            <div class="d-flex justify-content-center">
                <input id="dlpkg_{id}" type="button" value="Download" onclick="startPackageDownload({id})" class="btn btn-primary mx-2">
                <input id="rmpkg_{id}" type="button" value="Remove" onclick="removePackage({id})" class="btn btn-primary mx-2">
            </div>
        </div>
    </div>
</div>
`
pendingPackages = []
function getPackagesData() {
    $.ajax({
        url: '/api/Downloads/PendingPackages',
        type: 'GET',
        contentType: 'application/json',
    })
        .done(function (data) {
            //document.getElementById("pendingPackages").innerHTML = JSON.parse(result);
            var log = "";
            //var data = JSON.parse(result);
            var mainDiv = document.getElementById("pendingPackages");
            var existDiv = mainDiv.querySelectorAll(":scope > div");
            var idsPresent = [];
            for (var j = 0; existDiv && j < existDiv.length; j++) {
                found = false;
                for (var i = 0; !found && i < data.length; i++) {
                    if (data[i].id == existDiv[j].id) {
                        found = true;
                        idsPresent.push(data[i].id);
                        log += "Keeping " + data[i].id + ", ";
                    }
                }
                if (!found) {
                    log += "Removing " + existDiv[i].id + ", ";
                    mainDiv.removeChild(existDiv[j]);
                }
            }
            for (var i = 0; i < data.length; i++) {
                if (!idsPresent.includes(data[i].id)) {
                    //var el = templatePendingPackage.format(data[i].Name, data[i].Id);
                    var el = templatePendingPackage
                        .replace(/\{name\}/g, data[i].name)
                        .replace(/\{id\}/g, data[i].id)
                        .replace(/\{size\}/g, data[i].size)
                        .replace(/\{unit\}/g, data[i].unit);
                    mainDiv.innerHTML += el;
                    log += "Adding " + data[i].id + ", ";
                }
            }

            console.log(log);
        })
        .fail(function (xhr, textStatus, errorThrown) {
            console.log("Error refreshing pending packages: " + xhr.responseText);
        })
        .always(function () {
            setTimeout(getPackagesData, refreshInterval);
        });
}

function startPackageDownload(uuid) {
    $.ajax({
        url: '/api/Downloads/Start',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify([uuid]),
        headers: {
            RequestVerificationToken: document.getElementById('RequestVerificationToken').value
        }
    })
        .fail(function (xhr, textStatus, errorThrown) {
            alert("Error starting download: " + xhr.responseText);
        })
        .done(function (result) {

        });
}

function startAllPackagesDownload() {
    var mainDiv = document.getElementById("pendingPackages");
    var existDiv = mainDiv.querySelectorAll(":scope > div");
    var ids = [];
    for (var i = 0; i < existDiv.length; i++) {
        ids.push(existDiv[i].id);
    }

    $.ajax({
        url: '/api/Downloads/Start',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(ids),
        headers: {
            RequestVerificationToken: document.getElementById('RequestVerificationToken').value
        }
    })
        .fail(function (xhr, textStatus, errorThrown) {
            alert("Error starting download: " + xhr.responseText);
        })
        .done(function (result) {

        });
}

function removeAllPackages() {

}

function removePackage(uuid) {
    $.ajax({
        url: '/api/Downloads/RemovePendingPackages',
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify([uuid]),
        headers: {
            RequestVerificationToken: document.getElementById('RequestVerificationToken').value
        }
    })
        .fail(function (xhr, textStatus, errorThrown) {
            alert("Error removing download: " + xhr.responseText);
        })
        .done(function (result) {

        });
}

// !! DO NOT REMOVE THE $ -> AJAX SAVE OF RESPONSE DATA BREAKS!
$seasonData = [];
function showSeriesSelector() {
    if (selectedCards.length === 0) {
        $('#noSeriesSelected').modal();
        return;
    }

    $.ajax({
        url: '/api/Video/Series',
        type: 'GET',
        contentType: 'application/json',
        async: true
    })
        .done(function (data) {
            selector = document.getElementById("seriesSelector");
            selector.options.length = 0;
            for (i = 0; i < data.length; i++) {
                selector.options[i] = new Option(data[i].name, data[i].id);
                selector.options[i].tag = data[i].lastSelectedSeason;
            }
            document.getElementById("season").value = data[0].lastSelectedSeason;
            document.getElementById("seriesMoverName").innerText = selectedCards[0];
            data.forEach(d => $seasonData.push(d));
            $('#seriesSelectorModal').modal();
        })
        .fail(function (xhr, textStatus, errorThrown) {
            alert("Error fetching series: " + xhr.responseText);
        });
}

function showAddLinksModal() {
    $('#addLinksModal').modal();
}

function addLinks() {
    var textel = document.getElementById("txtarea_links");
    var text = textel.value;
    if (text.length === 0) {
        alert("No Links entered");
        return;
    }
    $.ajax({
        url: '/api/Downloads/AddUrls',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(text),
        headers: {
            RequestVerificationToken: document.getElementById('RequestVerificationToken').value
        }
    })
        .fail(function (xhr, textStatus, errorThrown) {
            alert("Error adding links: " + xhr.responseText);
        })
        .done(function (result) {
            textel.value = "";
        });
}

function updateSelectedSeason() {
    document.getElementById("season").value = $seasonData[document.getElementById("seriesSelector").selectedIndex].LastSelectedSeason; // document.getElementById("seriesSelector").value.tag;
}

function moveSeries() {
    var ss = document.getElementById("seriesSelector");
    var dto = {
        SeriesId: +ss.options[ss.selectedIndex].value,
        Season: +document.getElementById("season").value,
        Downloads: []
    };
    for (var i = 0; i < selectedCards.length; i++) {
        dto.Downloads.push(selectedCards[i]);
    }

    $.ajax({
        url: '/api/Video/MoveToSeries',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(dto),
        headers: {
            RequestVerificationToken: document.getElementById('RequestVerificationToken').value
        }
    })
        .fail(function (xhr, textStatus, errorThrown) {
            alert("Error adding move job: " + xhr.responseText);
        });
}

function moveMovies() {
    if (selectedCards.length === 0) {
        $('#noMoviesSelected').modal();
        return;
    }

    var dto = [];
    for (var i = 0; i < selectedCards.length; i++) {
        dto.push(selectedCards[i]);
    }

    $.ajax({
        url: '/api/Video/MoveToMovies',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(dto),
        headers: {
            RequestVerificationToken: document.getElementById('RequestVerificationToken').value
        }
    })
        .fail(function (xhr, textStatus, errorThrown) {
            alert("Error adding move job: " + xhr.responseText);
        });
}

function removeDownloads() {
    if (selectedCards.length === 0) {
        $('#noDownloadsSelected').modal();
        return;
    }

    var dto = [];
    for (var i = 0; i < selectedCards.length; i++) {
        dto.push(selectedCards[i]);
    }
    $.ajax({
        url: '/api/Downloads/Remove',
        type: 'DELETE',
        contentType: 'application/json',
        data: JSON.stringify(dto),
        headers: {
            RequestVerificationToken: document.getElementById('RequestVerificationToken').value
        }
    })
        .fail(function (xhr, textStatus, errorThrown) {
            alert("Error adding delete job: " + xhr.responseText);
        });
}

function getMoveOps() {
    $.ajax({
        url: '/api/Video/FileOperationStates',
        type: 'GET',
        contentType: 'application/json',
    })
        .done(function (seasonData) {
            var el = document.getElementById("moveOps");
            //seasonData = JSON.parse(result);
            if (seasonData.length === 0) {
                el.innerHTML = "<i>No pending operations...</i>";
            } else {
                var str = '<ul  class="list-group list - group - flush">';
                for (var i = 0; i < seasonData.length; i++) {
                    var clattr = "primary";
                    if (seasonData[i].currentState === "Success") {
                        clattr = "success"
                    } else if (seasonData[i].currentState === "Failed") {
                        clattr = "danger";
                    }
                    str += '<li id="fmo_' + seasonData[i].id + '" class="list-group-item list-group-item-' + clattr + ' d-flex justify-content-between align-items-center">' + seasonData[i].value + ": " + seasonData[i].currentState;
                    if (clattr === "danger") {
                        str += " (" + seasonData[i].errorMessage + ")";
                    }
                    if (clattr !== "primary") {
                        str += '<span class="badge badge-' + clattr + ' badge-pill linkCursor" onclick="dismissFMO(' + seasonData[i].id + ');">&times;</span>';
                    }
                    str += "</li>";
                }
                str += "</ul>";
                el.innerHTML = str;
            }
            setTimeout(getMoveOps, 1000);
        })
        .fail(function (xhr, textStatus, errorThrown) {
            console.log("Error refreshing move state: " + xhr.responseText);
            setTimeout(getMoveOps, 1000);
        });
}

function dismissFMO(id) {
    $.ajax({
        url: '/api/Video/DismissFmo',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(id),
        customId: id,
        headers: {
            RequestVerificationToken: document.getElementById('RequestVerificationToken').value
        }
    }).done(function (result) {
        var li = document.getElementById("fmo_" + this.customId);
        li.parentNode.removeChild(li);
    })
        .fail(function (xhr, textStatus, errorThrown) {
            console.log("Error dismissing move operation state: " + xhr.responseText);
        });
}

function restartDownloads() {
    $.ajax({
        url: '/api/Downloads/Restart',
        type: 'POST',
        contentType: 'application/json',
        headers: {
            RequestVerificationToken: document.getElementById('RequestVerificationToken').value
        }
    }).fail(function (xhr, textStatus, errorThrown) {
            console.log("Error restarting downloads: " + xhr.responseText);
        });
}

function showHistory() {
    $.ajax({
        url: '/api/Downloads/UrlHistory',
        type: 'GET',
    }).done(function (hist) {
        modhtml = "";
        for (var i = 0; i < hist.length; i++) {
            createdDate = new Date();
            createdDate.setTime(hist[i].created*1000);
            histhtml = '<div class="card" style="margin-bottom:10px"><div class="card-header">';
            histhtml += createdDate.toUTCString();
            histhtml += '</div><ul class="list-group list-group-flush" style="font-size:smaller"><li class="list-group-item">';
            for (var j = 0; j < hist[i].data.length; j++) {
                histhtml += hist[i].data[j] + "<br />";
            }
            histhtml += '</li><li class="list-group-item"><a href="#" onclick="resubmitLinks(';
            histhtml += hist[i].id;
            histhtml += ');">Resubmit</a></li></ul></div>';
            modhtml += histhtml;
        }
        document.getElementById("historymodal_body").innerHTML = modhtml;
        $('#historyModal').modal();
    }).fail(function (xhr, textStatus, errorThrown) {
        alert(xhr.responseText);
    });
}

function resubmitLinks(hist_id) {
    $.ajax({
        url: '/api/Downloads/ResubmitUrls',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(hist_id),
        headers: {
            RequestVerificationToken: document.getElementById('RequestVerificationToken').value
        }
    }).fail(function (xhr, textStatus, errorThrown) {
        console.log("Error resubmitting links: " + xhr.responseText);
        alert("Error resubmitting links: " + xhr.responseText);
    });
}


getDownloadData();
getMoveOps();
getPackagesData();
getDownloadControllerStatus()