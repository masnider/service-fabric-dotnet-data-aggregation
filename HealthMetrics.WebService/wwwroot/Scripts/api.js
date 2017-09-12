﻿function Api() {
    this.serviceUrl = location.protocol + '//' + location.hostname + (location.port ? ':' + location.port : '');

    this.GetNationalStats = function (result) {
        this.httpGetJson(this.serviceUrl + '/api/national/stats', result);
    };

    this.GetNationalHealth = function (result) {
        this.httpGetJson(this.serviceUrl + '/api/national/health', result);
    };

    this.GetCountyHealth = function (countyId, result) {
        this.httpGetJson(this.serviceUrl + '/api/county/' + countyId + '/health', result);
    };

    this.GetDoctors = function (countyId, result) {
        this.httpGetJson('/api/county/' + countyId + '/doctors', result);
    };

    this.GetPatient = function (bandId, result) {
        this.httpGetJson(this.serviceUrl + '/api/patients/' + bandId, result);
    };

    this.GetSetting = function (settingName, result) {
        this.httpGetJson(this.serviceUrl + '/api/settings/' + settingName, result);
    };

    this.GetIds = function (result) {
        this.httpGetJson(this.serviceUrl + '/api/GetIds', result);
    };

    this.httpGetJson = function (url, result) {
        $.ajax({
            headers: {
                //"Accept":"application/x-protobuf",
                //"Content-Type":"application/x-protobuf"
                "Accept":"application/json",
                "Content-Type":"application/json"
            },
            url: url,
            type: 'GET',
            //datatype: 'protobuf',
            cache: false
        })
	   .done(function (data) {
	       result(data);
	   })
	   .fail(function () {
	       return;
	   });
    }
}