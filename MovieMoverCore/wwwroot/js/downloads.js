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
        .always(function () {
            setTimeout(getDownloadData, 2000);
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
        url: '/Downloads?handler=Series',
        type: 'GET',
        contentType: 'application/json',
        async: true,
        success: function (result) {
            data = JSON.parse(result);
            selector = document.getElementById("seriesSelector");
            selector.options.length = 0;
            for (i = 0; i < data.length; i++) {
                selector.options[i] = new Option(data[i].Name, data[i].Id);
                selector.options[i].tag = data[i].LastSelectedSeason;
            }
            document.getElementById("season").value = data[0].LastSelectedSeason;
            document.getElementById("seriesMoverName").innerText = selectedCards[0];
            data.forEach(d => $seasonData.push(d));
            $('#seriesSelectorModal').modal();

        }
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
        url: '/Downloads?handler=MoveSeries',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(dto),
        headers: {
            RequestVerificationToken: document.getElementById('RequestVerificationToken').value
        }
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
        url: '/Downloads?handler=MoveMovies',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(dto),
        headers: {
            RequestVerificationToken: document.getElementById('RequestVerificationToken').value
        }
    });
}

function getMoveOps() {
    $.ajax({
        url: '/Downloads?handler=MoveStates',
        type: 'GET',
        contentType: 'application/json',
    })
        .done(function (result) {
            var el = document.getElementById("moveOps");
            seasonData = JSON.parse(result);
            if (seasonData.length === 0) {
                el.innerHTML = "<i>No pending move operations...</i>";
            } else {
                var str = '<ul  class="list-group list - group - flush">';
                for (var i = 0; i < seasonData.length; i++) {
                    var clattr = "primary";
                    if (seasonData[i].CurrentState === "Success") {
                        clattr = "success"
                    } else if (seasonData[i].CurrentState === "Failed") {
                        clattr = "danger";
                    }
                    str += '<li id="fmo_' + seasonData[i].ID + '" class="list-group-item list-group-item-' + clattr + ' d-flex justify-content-between align-items-center">' + seasonData[i].Name + ": " + seasonData[i].CurrentState;
                    if (clattr === "danger") {
                        str += " (" + seasonData[i].ErrorMessage + ")";
                    }
                    if (clattr !== "primary") {
                        str += '<span class="badge badge-' + clattr + ' badge-pill linkCursor" onclick="dismissFMO(' + seasonData[i].ID + ');">&times;</span>';
                    }
                    str += "</li>";
                }
                str += "</ul>";
                el.innerHTML = str;
            }
            setTimeout(getMoveOps, 1000);
        });
}

function dismissFMO(id) {
    $.ajax({
        url: '/Downloads?handler=DismissFmo',
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
    });
}

getDownloadData();
getMoveOps();