emaDemos.controller('ForecastController', function ($scope, $http, blockUI) {
    $scope.$http = $http;

    $scope.postUrl = {
        "Data": "/Prices/HistoricalSystemPrice?refresh=false&resolution=Daily",
        "Valuation": "/Valuations/PowerPurchaseAgreement"
    };

    $scope.onChangedPostResponse = { "Data": dataSuccess, "Valuation": valuationSuccess };

    $scope.currentFrameId = "Valuation"; //all changes are posted there...
    //prepare variables... put somewhere...
    $scope.httpPostParameters = [];
    $scope.objectWatchers = {};

    registerParameter($scope, 'retailPrice', 30);
    registerParameter($scope, 'horizon', 3);
    registerParameter($scope, 'capacity', 100);
    registerParameter($scope, 'margin', 5);
    registerParameter($scope, 'confidence', 0.995);
    
    $scope.Currency = "€";

    var chart = $('#container').highcharts('StockChart', {
        chart: {
            zoomType: 'xy'
        },
        tooltip: {
            valueDecimals: 4
        },
        rangeSelector: {
            selected: 1
        },
        title: {
            text: 'Simulations'
        },
        xAxis: {
            ordinal: false,
            type: 'datetime'
            //,tickInterval: 86400000
        },
        legend: {
            layout: 'vertical',
            align: 'right',
            verticalAlign: 'top',
            borderWidth: 0
        },
        series: []
    });

    window.chart = chart = $('#container').highcharts();
    
    /* Private methods */
    function prcFrmt(prc) {
        return (prc*100).toFixed(2);
    }

    var startingDate, startingDateHistorical;

    function valuationSuccess(dataObj) {
        clearSeriesContainingName(chart, "Simulated");

        var histChart = createHistogram();
        plotDataToHistogram(histChart, dataObj.Histogram);

        /* Do forecast */
        var simulations = dataObj.Simulations;
        
        for (var i = 0; i < simulations.length; i++) {
            var simulation = simulations[i];
            var simulationData = [];
            for (var j = 0; j < simulation.length; j++) {
                simulationData[j] = [startingDate + j * TICKS_IN_DAY, simulation[j]];
            }

            var serie = {
                name: "Simulated Path " + i,
                color: "#FF0000",
                dashStyle: "ShortDot",
                data: simulationData
            };

            chart.addSeries(serie);
        }
        
        
        //todo: fix UTC...
        var startDate = new Date(startingDateHistorical);
        //Adjust for utc and include eventual backcasting
        startDate.setTime(startDate.getTime() + (2 * 60 * 60 * 1000));
        var rangeStartDate = Date.parse(startDate);

        var endDate = new Date(startingDate);
        //Must show only until hour 23
        endDate.setTime(endDate.getTime() + (1 * 60 * 60 * 1000) + $scope.horizon * 365 * TICKS_IN_DAY);
        var rangeEndDate = Date.parse(endDate);

        chart.xAxis[0].setExtremes(rangeStartDate, rangeEndDate);

        $scope.extendWith('Results', dataObj);
        $scope.extendWithAndListen('MathModel', dataObj);

        blockUI.stop();
    }
    
    function dataSuccess(data) {
        startingDateHistorical = Date.parse(data[0].DateTime);
        startingDate = Date.parse(data[data.length - 1].DateTime);

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
            },
            zIndex: 9
        };

        chart.addSeries(serie);
        
        doPageParametersPost($scope, "Valuation");
    }
    
    /* Start all */
    doPageParametersPost($scope, "Data");
});