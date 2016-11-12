emaDemos.controller('StorageValuationController', function ($scope, $http) {
    //abstract charts to a projects called .WebUtils... all forecasting and graphs must be there...
    //and maybe used as components... GREAT IDEA!

    $scope.date = new Date(2015, 7, 3); //js months start at 0..
    $scope.TimeHorizons = TH;
    $scope.timeHorizon = TH[0];
    $scope.ForecastMethods = FM;
    $scope.forecastMethod = FM[0];
    $scope.Currency = "€";

    var chart = $('#container').highcharts('StockChart', {
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
    function isLoadFirstTime() {
        return $scope.ForecastMethods.length == 0;
    }

    function getSuccess(dataObj) {
        clearSeriesContainingName("Forecast");
        clearSeriesContainingName("Confidence");

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
            dashStyle: "ShortDot",
            data: data
        };

        chart.addSeries(serie);

        var confidences = dataObj.Result.Confidence;

        for (var i = 0; i < confidences.length; i++) {
            var confidenceSerie = confidences[i];
            var confidenceValues = [];
            for (var j = 0; j < confidenceSerie.length; j++) {
                confidenceValues[j] = [dt + (j + 2) * TICKS_IN_HOUR, confidenceSerie[j]];
            }

            var serie = {
                name: "Confidence " + ((i === 0) ? "Upper" : "Lower"),
                color: "#FF0000",
                type: 'spline',
                dashStyle: "Solid",
                data: confidenceValues
            };

            chart.addSeries(serie);
        }

        chart.rangeSelector.clickButton(0);

        angular.extend($scope, dataObj.BaseFit); //or merge the whole data object...?
    }

    function loadForecast() {
        var th = $scope.TimeHorizons.indexOf($scope.timeHorizon);

        $http.get('/Prices/Forecast?priceSeries=Elspot-SystemPrice&forecastHorizon=' + th
            + '&method=Naive&dateTime='
            + $scope.date.toISOString()) //default parameters?...
        .success(getSuccess)
        .error(function (status) {

        });
    }

    function loadHistoricalData() {
        $http.get('/Prices/HistoricalSystemPrice?refresh=false&resolution=5').success(function (data) {
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

            loadForecast();
        });
    }

    function varChanged(newval, oldval) {
        if (newval == oldval || newval == undefined || oldval == undefined)
            return;

        loadForecast();
    }

    /* Methods of controller */
    $scope.changeDropdown = function (dropdown, value) {
        $scope[dropdown] = value;
    }

    $scope.$watch('forecastMethod', varChanged);
    $scope.$watch('timeHorizon', varChanged);

    /* Start all */
    loadHistoricalData()
});