﻿var emaDemos = angular.module('emaDemos', ['ngRoute', 'ui.bootstrap', 'blockUI']);

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
            if (scope[element.attr("ng-model")+"Min"] != undefined && scope[element.attr("ng-model")+"Min"].constructor == Date)
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

TICKS_IN_SECOND = 86400000;