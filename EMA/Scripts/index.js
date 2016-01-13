$(function () {
    var chart = $('#container').highcharts('StockChart', {
        tooltip: {
            valueDecimals: 4
        },
        rangeSelector: {
            selected: 1
        },
        title: {
            text: 'Nordic Forward Contracts... :D'
        },
        xAxis: {
            ordinal: false,
            type: 'datetime'
            //,tickInterval: 86400000
        },
        series: []
    });
    
    //sorta global variables...
    window.chart = chart = $('#container').highcharts(); //
    window.forwardLevels = [];
    window.timeSteps = [];
    window.startingDateForwardCurve;
    var timeStep = 0; //hourly, daily, monthly, quarterly, yearly... 

    $.getJSON('/Prices/ForwardCurve?region=nordic&nonOverlapping=true', function (data) {
        startingDateForwardCurve = Date.parse(data[0].Begin);

        for (var i = 0; i < data.length; i++) {
            var fwd = data[i];

            var price = fwd.FixPrice;
            var begin = Date.parse(fwd.Begin);
            var end = Date.parse(fwd.End);
            //this is daily, timeStep = 86400000
            var diff = Math.ceil((end - begin) / 86400000);

            var priceData = [];
            forwardLevels.push(price);
            timeSteps.push(diff + 1);

            //we must replicate the price daily, so the server won't have that high of a burden
            for (var j = 0; j <= diff; j++) {
                priceData[j] = [begin + j * 86400000, price];
            }
            
            var serie = {
                name: fwd.Contract,
                data: priceData
            };

            chart.addSeries(serie);
            
            chart.rangeSelector.clickButton(5);
        }
        $("#container").trigger("ForwardContractsReady");
    });

    //$.getJSON('/Prices/ForwardCurveInterpolation?algo=empirical', function (data) {
        
    //})

    $.getJSON('/Prices/HistoricalSystemPrice?refresh=false', function(data) {
        var priceData = [];
        for (var i = 0; i < data.length; i++) {
            //try using linq.js => YES!
            var dt = Date.parse(data[i].DateTime);
            priceData[i] = [dt, data[i].Value];
        }

        var serie = {
            name: "Historical Price",
            color: '#CFCFCF',
            data: priceData,
            tooltip: {
                valueDecimals: 4
            }
        };

        chart.addSeries(serie);

        chart.rangeSelector.clickButton(5);
    });
   
});