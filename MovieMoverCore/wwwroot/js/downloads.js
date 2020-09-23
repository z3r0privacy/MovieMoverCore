
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
            document.getElementById("downloadCards").innerHTML = JSON.parse(result);
            
    });
}

function showSeriesSelector() {
    if (selectedCards.length === 0) {
        $('#noSeriesSelected').modal();
        return;
    }

    $.ajax({
            url: '/Downloads?handler=Series',
            type: 'GET',
            contentType: 'application/json',
        })
            .done(function (result) {
                data = JSON.parse(result);
                selector = document.getElementById("seriesSelector");
                selector.options.length = 0;
                for (i = 0; i < data.length; i++) {
                    selector.options[i] = new Option(data[i].Name, data[i].Id);
                }
                document.getElementById("season").value = 1;
                $('#seriesSelectorModal').modal();
        });
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
    })
        .done(function (result) {
            alert(result);
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
            data = JSON.parse(result);
            if (data.length === 0) {
                el.innerHTML = "<i>No pending move operations...</i>";
            } else {
                var str = "<ul>";
                for (var i = 0; i < data.length; i++) {
                    str += "<li>" + data[i].Name + ": " + data[i].CurrentState;
                    if (data[i].ErrorMessage !== "") {
                        str += " (" + data[i].ErrorMessage + ")";
                    }
                    str += "</li>";
                }
                str += "</ul>";
                el.innerHTML = str;
            }
            setTimeout(getMoveOps, 1000);
        });
}

getDownloadData();
getMoveOps();