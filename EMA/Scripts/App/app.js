var emaDemos = angular.module('emaDemos', ['ngRoute', 'ui.bootstrap', 'blockUI']);

emaDemos.directive('datetimez', function () {
    return {
        restrict: 'A',
        require: 'ngModel',
        link: function (scope, element, attrs, ngModelCtrl) {
            var today = moment(new Date());

            var defaultDate = today, maxDate = today;
            var minDay = moment(new Date(2014, 1, 1));

            if (scope[element.attr("ng-model")] != undefined && scope[element.attr("ng-model")].constructor == Date)
                defaultDate = moment(scope[element.attr("ng-model")]);
            if (scope[element.attr("ng-model") + "Max"] != undefined && scope[element.attr("ng-model") + "Max"].constructor == Date)
                maxDate = moment(scope[element.attr("ng-model")+"Max"]);
            if (scope[element.attr("ng-model") + "Min"] != undefined && scope[element.attr("ng-model") + "Min"].constructor == Date)
                minDay = moment(scope[element.attr("ng-model")+"Min"]);

            element.datetimepicker({
                showClose: true,
                format: "DD/MMM/YYYY",
                viewMode: "days",
                showClose: true,
                //disabledHours: true,
                minDate: minDay,
                maxDate: maxDate,
                defaultDate: defaultDate
            }).on('dp.change', function (e) {
                //close date picker

                e = e.date;
                if (e.constructor != Date)
                    e = e._d;
                
                //send event further
                ngModelCtrl.$setViewValue(e);
                scope.$apply();
            });
        }
    };
});

//emaDemos.run(function ($scope) {
//    $scope.changeDropDownValue = function (dropdown, value) {
//        $scope[dropdown] = value;
//    }
//});

TICKS_IN_DAY = 86400000;

TICKS_IN_HOUR = 3600000;

//correction for GMT thingy...
GMT = (new Date()).getHours() - (new Date()).getUTCHours();

emaDemos.controller('ArbsController', function ($scope, $http) {
    
    function getForwardCurve() {
        $.getJSON('/EnergyMarkets/ArbVals', function (data) {
            $scope.Arbitrages = data;
            $scope.$apply();
        });
    }

    getForwardCurve();
});