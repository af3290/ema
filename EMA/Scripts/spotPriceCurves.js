$(function () {
    var chart = $('#container').highcharts('StockChart', {
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
        //,tooltip: {
        //    valueDecimals: 2
        //}
    });
    chart = $('#container').highcharts();

    var timeStep = 0; //hourly, daily, monthly, quarterly, yearly... 

    $.getJSON('/Prices/ForwardCurve?region=nordic&nonOverlapping=true', function (data) {
        for (var i = 0; i < data.length; i++) {
            var fwd = data[i];

            var price = fwd.FixPrice;
            var begin = Date.parse(fwd.Begin);
            var end = Date.parse(fwd.End);
            var diff = Math.ceil((end - begin) / 86400000);

            var priceData = [];

            //we must replicate the price daily, so the server won't have that high of a burden
            for (var j = 0; j <= diff; j++) {
                priceData[j] = [begin + j * 86400000, price];
            }

            var serie = {
                name: fwd.Contract,
                data: priceData,
                tooltip: {
                    valueDecimals: 4
                },
            };

            chart.addSeries(serie);
            //chart.redraw();
        }
    });
});