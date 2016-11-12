emaDemos.controller('PlantPortfolioController', function ($scope, $http) {
    //abstract charts to a projects called .WebUtils... all forecasting and graphs must be there...
    //and maybe used as components... GREAT IDEA!

    $scope.date = new Date(2015, 7, 3); //js months start at 0..
   
    $scope.Currency = "€";

    

    window.chart = chart = $('#container-var').highcharts();

    /* Private methods */

    function getSuccess(dataObj) {
        
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