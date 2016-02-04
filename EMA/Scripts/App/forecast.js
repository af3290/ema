emaDemos.controller('ForecastController', function ($scope, $http, blockUI) {
    $scope.$http = $http;

    $scope.postUrl = {
        "Data": "/Prices/HistoricalSystemPrice?refresh=false&resolution=5",
        "Forecast": "/Prices/Forecast"
    };

    $scope.onChangedPostResponse = { "Data": dataSuccess, "Forecast": forecastSuccess };

    $scope.currentFrameId = "Forecast"; //all changes are posted there...
    //prepare variables... put somewhere...
    $scope.httpPostParameters = [];
    $scope.objectWatchers = {};

    registerParameter($scope, 'date', new Date(2015, 7, 3));
    registerParameter($scope, 'forecastMethod', $scope.ForecastMethods[0]);
    registerParameter($scope, 'spikesPreprocessMethod', $scope.SpikesPreprocessMethods[0]);
    registerParameter($scope, 'timeHorizon', $scope.TimeHorizons[0]);
    registerParameter($scope, 'confidence', 0.60);
    registerParameter($scope, 'exogenousVariables', {});
    
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
            text: 'Forecast'
        },
        xAxis: {
            ordinal: false,
            type: 'datetime'
            //,tickInterval: 86400000
        },
        series: []
    });

    window.chart = chart = $('#container').highcharts();
    
    /* Private methods */
    function prcFrmt(prc) {
        return (prc*100).toFixed(2);
    }

    function forecastSuccess(dataObj) {
        clearSeriesContainingName(chart, "Backcast");
        clearSeriesContainingName(chart, "Forecast");
        clearSeriesContainingName(chart, "Confidence");

        /* Do forecast */
        var forecast = dataObj.Result.Forecast;
        
        var dt = Date.parse($scope.date);
        var data = [];
        for (var j = 0; j < forecast.length; j++) {
            //TODO: fix the +2 error, GMT? or why?
            data[j] = [dt + (j + 2) * TICKS_IN_HOUR, forecast[j]];
        }
                
        var serie = {
            name: "Forecast ",
            color: "#FF0000",
            dashStyle: "ShortDash",
            data: data,
            zIndex: 9
        };
                
        chart.addSeries(serie);

        /* Do eventual backcast */
        var backcast = dataObj.Result.Backcast;
        var backcastData = [];
        for (var j = 0; j < backcast.length; j++) {
            //TODO: fix the +2 error, GMT? or why?
            backcastData[j] = [dt + (j + 2 - backcast.length) * TICKS_IN_HOUR, backcast[j]];
        }

        var backcastSerie = {
            name: "Backcast ",
            color: "#0000FF",
            dashStyle: "LongDash",
            data: backcastData,
            lineWidth: 0.25,
            zIndex: 9
        };

        chart.addSeries(backcastSerie);

        /* Do confidences */
        var confidences = dataObj.Result.Confidence;
        var confidenceBand = [];
        var bandCount = confidences.length / 2;

        //add confidence values...
        for (var i = 0; i < confidences.length; i++) {
            var confidenceSerie = confidences[i];
            var confidenceValues = [];

            //first upper, then lower
            for (var j = 0; j < confidenceSerie.length; j++) {
                var dVal = dt + (j + 2) * TICKS_IN_HOUR;
                var cVal = confidenceSerie[j];

                confidenceValues[j] = [dVal, cVal];
                
                //start doing the bands when lower intervals are processed...
                if (i >= bandCount) {
                    var subi = confidences.length - i - 1;
                    confidenceBand[subi][j] = [dVal, confidences[subi][j], cVal]
                } else {
                    //initialize
                    confidenceBand[i] = [];
                }
            }

            var serie = {
                name: "Confidence " + ((i < bandCount) ? "Upper" : "Lower")
                    + prcFrmt(dataObj.Result.ConfidenceLevels[i % bandCount]) + ' %',
                color: "#FF0000",
                type: 'line',
                dashStyle: "ShortDot",
                data: confidenceValues,
                zIndex: 9,
                enableMouseTracking: false
            };

            //chart.addSeries(serie);
        }

        //starts from narrower to winder
        for (var i = 0; i < bandCount; i++) { 
            /* Add confidence bands areas */
            var cofidenceBandArea = {
                name: 'Confidence Band ' + prcFrmt(dataObj.Result.ConfidenceLevels[(bandCount - i - 1)]) + ' %',
                data: confidenceBand[i],
                type: 'arearange',
                lineWidth: 0,
                linkedTo: ':previous',
                color: '#FF0000',
                fillOpacity: 0.05 + (bandCount - i) * 0.05,
                zIndex: (bandCount - i)
            };

            chart.addSeries(cofidenceBandArea);
        }

        //todo: fix UTC...
        var startDate = new Date($scope.date);
        //Adjust for utc and include eventual backcasting
        startDate.setTime(startDate.getTime() + (2 * 60 * 60 * 1000) - backcast.length * TICKS_IN_HOUR);
        var rangeStartDate = Date.parse(startDate);

        var endDate = new Date($scope.date);
        //Must show only until hour 23
        endDate.setTime(endDate.getTime() + (1 * 60 * 60 * 1000) + dataObj.DaysAhead * 24 * TICKS_IN_HOUR);
        var rangeEndDate = Date.parse(endDate);

        chart.xAxis[0].setExtremes(rangeStartDate, rangeEndDate);

        $scope.extendWith('Fit', dataObj);
        $scope.extendWith('BaseFit', dataObj);
        $scope.extendWith('PeakFit', dataObj);
        $scope.extendWithAndListen('MathModel', dataObj);
        
        blockUI.stop();
    }
                
    function dataSuccess(data) {
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
        
        $scope.ForecastSurfaceIframeUrl = "/Plotly/ProbabilitySurface?series=ElsSystem&bins=6&period=24&distributionType=empirical";

        doPageParametersPost($scope, "Forecast");
    }
    
    /* Start all */
    doPageParametersPost($scope, "Data");
});