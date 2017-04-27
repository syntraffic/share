(function ($) {
    var HarView = function (element, options) {
        var reqTemplate = "<div id='{{id}}-req' class='request' data-id='{{id}}' data-order='{{id}}' data-size='{{rawSize}}'>\
            <span class='plus' id='{{id}}'>&nbsp;&nbsp;&nbsp;</span>\
            <span class='url {{statusClass}}' id='{{id}}-url' title='{{request.url}}'>{{request.url}}</span>\
            <span class='bodySize' title='Response Size' id='{{id}}-bodySize'>{{size}}</span>\
            <span class='time' title='Response time' id='{{id}}-time'>{{time}}</span>\
            <span class='timelineBar' id='{{id}}-timeline' title='{{time_title}}'>{{{timeline}}}</span>\
        </div>";
        var summaryTemplate = "<div id='summary' class='summary'><span class='total-requests' id='total_requests'></span>" +
            "<span class='total-size' id='total_size'></span><span class='total-size' id='total_time'></span></div>";

        var detailsTemplate = "<div class='details hidden' id='{{id}}-details'>\
            <td colspan='7'>\
                <div id='{{id}}-tabs' class='tabs' data-disable='{{disabled_tabs}}'>\
                    <ul>\
                        <li><a href='#{{id}}-tab-0'>Headers</a></li>\
                        <li><a href='#{{id}}-tab-1'>Params</a></li>\
                        <li><a href='#{{id}}-tab-2'>Request</a></li>\
                        <li><a href='#{{id}}-tab-3'>Response</a></li>\
                    </ul>\
                    <div id='{{id}}-tab-0'>\
                        <span class='btn {{statusClass}}'>{{response.status}}</span>\
                        <p class='header'>Request headers</p>\
                        <div id='{{id}}-req-headers'>{{{rq_headers}}}</div>\
                        <p class='header'>Response headers</p>\
                        <div id='{{id}}-resp-headers'>{{{rs_headers}}}</div>\
                    </div>\
                    <div id='{{id}}-tab-1'>\
                        <pre id='{{id}}-query-string' class='body'>{{{query}}}</pre>\
                    </div>\
                    <div id='{{id}}-tab-2'>\
                        <pre id='{{id}}-req-body' class='body'>{{request.postData.text}}</pre>\
                    </div>\
                    <div id='{{id}}-tab-3'>\
                        <pre id='{{id}}-resp-body' class='body'>{{response.content.text}}</pre>\
                    </div>\
                </div>\
            </td>\
        </div>";

        var headersTemplate = "<table>\
            {{#headers}}\
            <tr>\
                <td>{{name}}:</td>\
                <td>{{value}}</td>\
            </tr>\
            {{/headers}}\
        </table>";

        var timingsTemplate = "<span id='{{id}}-lpad' class='timelinePad' style='width:{{timings._lpad}}%'></span><span\
          id='{{id}}-blocked' class='timelineSlice timelineBlocked' style='width:{{timings.blocked}}%;'></span><span\
          id='{{id}}-dns' class='timelineSlice timelineDns' style='width:{{timings.dns}}%'></span><span\
          id='{{id}}-connect' class='timelineSlice timelineConnect' style='width:{{timings.connect}}%'></span><span\
          id='{{id}}-send' class='timelineSlice timelineSend' style='width:{{timings.send}}%'></span><span\
          id='{{id}}-wait' class='timelineSlice timelineWait' style='width:{{timings.wait}}%'></span><span\
          id='{{id}}-receive' class='timelineSlice timelineReceive' style='width:{{timings.receive}}%'></span><span\
          id='{{id}}-rpad' class='timelinePad' style='width:{{timings._rpad}}%'></span>";

        $(element).addClass('har');
        $(element).append($(summaryTemplate));

        var log = {
            entries: {}
        };
        var totals = {};
        var pads = {};
        var left, right;
        var idctr = 0;
        var reqCount = 0;
        var totalReqSize = 0;
        var totalRespSize = 0;
        var total4xx = 0;
        var total5xx = 0;
        var domainRequests = {};
        var resources = {};

        var processingt = 0;
        var reqt = 0;
        var respt = 0;
        var rendert = 0;
        var timingt = 0;

        this.render = function (har, $target) {
            var that = this;
            var pageref;
            if (har && har.log && har.log.pages && har.log.pages.length > 0) {
                //console.log("Start har construction");

                if (har.log.pages[0].pageTimings.onLoad === null) {
                    har.log.pages[0].pageTimings.onLoad = getLoadTime(har);
                }

                _updateField('#respTime', formatTime(har.log.pages[0].pageTimings.onLoad), $target);
                var cnt = 0;
                calCulateLeftAndRight(har.log.entries);
                $.each(har.log.entries, function (index, entry) {
                    pageref = pageref || entry.pageref;
                    if (entry.pageref === pageref) {

                        // elimitate requests that are local which apparently are returned by chrome. Example data:image/png;base64,iVBO
                        if (entry.request && entry.request.headers && entry.request.headers.length > 0) {
                            parseEntry(entry, idctr++, $target);
                        }
                    }
                });

                _updateField('#respSize', formatBytes(totalRespSize), $target);

                _updateField('#rsrcCount', reqCount, $target);

                $('#total_requests', $target).text('Total Requests ' + reqCount);
                $('#total_size', $target).text(formatBytes(totalRespSize));
                $('#total_time', $target).text(formatTime(har.log.pages[0].pageTimings.onLoad));

                _updateField('#successP', ((reqCount - total4xx - total5xx) * 100 / reqCount).toFixed(2) + '%', $target);
                if ((total4xx + total5xx) > 0) {
                    var title = '';
                    if (total4xx > 0) {
                        title += '4xx errors:  ' + total4xx;
                    }

                    if (total5xx > 0) {
                        if (title.length > 0) {
                            title += '<br/>';
                        }

                        title += '5xx errors:  ' + total5xx;
                    }

                    $('#successP', $target).attr('title', title).addClass('found-error');
                }

                $('.tabs', $target).each(function () {
                    var disabled = $(this).attr('data-disable').split(',').map(Number);
                    $(this).tabs({ disabled: disabled });
                });
                //console.log("Done har construction");
            }
        }

        function getLoadTime(har) {
            var startTime = new Date(har.log.pages[0].startedDateTime);
            var loadTime = 0;

            // Loop over all entries to determine the latest request end time
            // The variable 'har' contains the JSON of the HAR file
            har.log.entries.forEach(function (entry) {
                var entryLoadTime = new Date(entry.startedDateTime);
                // Calculate the current request's end time by adding the time it needed to load to its start time
                entryLoadTime.setMilliseconds(entryLoadTime.getMilliseconds() + entry.time);
                // If the current request's end time is greater than the current latest request end time, then save it as new latest request end time
                if (entryLoadTime > loadTime) {
                    loadTime = entryLoadTime;
                }
            });

            return loadTime - startTime;
        }

        this.getSize = function () {
            return totalRespSize;
        }


        this.getDomainData = function (maxItems) {
            return makeArrayFromObject(domainRequests, maxItems);
        }

        this.getResourcesData = function () {
            return makeArrayFromObject(resources, 5)
        }

        function makeArrayFromObject(sourceItems, maxItems) {
            var items = Object.keys(sourceItems).map(function (key) {
                return [key, sourceItems[key]];
            });

            items.sort(function (first, second) {
                return second[1].c - first[1].c;
            });

            if (items.length > maxItems) {
                var ret = items.slice(0, maxItems);
                ret[maxItems - 1] = ['Others', { c: ret[maxItems - 1][1].c, s: ret[maxItems - 1][1].s, t: ret[maxItems - 1][1].t }];

                for (var i = maxItems; i < items.length; i++) {
                    ret[maxItems - 1][1].c += items[i][1].c;
                    ret[maxItems - 1][1].s += items[i][1].s;
                    ret[maxItems - 1][1].t += items[i][1].t;
                }

                return ret;
            } else {
                return items;
            }
        }

        var _updateField = function (id, field, $target) {
            if (field) {
                $(id, $target).text(field);
            }
        }

        var _updateHeaders = function (id, isRequest, headers, $target) {
            var html = Mustache.to_html(headersTemplate, {
                headers: headers
            });

            $('#' + id + (isRequest ? '-req-headers' : '-resp-headers'), $target).append($(html));
        }

        var _updateQueryString = function (id, queryString, $target) {
            var html = Mustache.to_html(headersTemplate, {
                headers: queryString
            });

            $('#' + id + '-query-string', $target).append($(html));
        }


        function calCulateLeftAndRight(entries) {
            entries.forEach(function (entry) {
                var t = new Date(entry.startedDateTime).getTime();
                if (left && right) {
                    left = (left < t) ? left : t;
                    right = (right > t) ? right : t;
                }
                else {
                    left = right = t;
                }

                var total = 0;
                $.each(entry.timings, function (key, value) {
                    if (value > -1) {
                        total += value;
                    }
                });

                entry.total = total;
                t = t + total;
                right = (right > t) ? right : t;

            });
        }

        function parseEntry(entry, id, $target) {
            reqCount = reqCount + 1;

            var statusClass = '', btnStatus;
            if (entry.response.status < 399) {
                statusClass = 'resp2xx';
                btnStatus = 'btn-success';
            } else if (entry.response.status > 399 && entry.response.status < 500) {
                statusClass = 'resp4xx resperror';
                btnStatus = 'btn-danger';
                total4xx++;
            } else if (entry.response.status > 499 && entry.response.status < 600) {
                statusClass = 'resp5xx resperror';
                btnStatus = 'btn-danger';
                total5xx++;
            }
            var t = new Date(entry.startedDateTime).getTime();
            var total = entry.total;
            pads[id] = [t - left, right - t - total];
            totals[id] = total + pads[id][0] + pads[id][1];

            var timingsWidth = {};

            var frac = 100 / totals[id];
            $.each(entry.timings, function (key, value) {
                var width = (value < 0) ? 0 : value;
                if (width > 0) {
                    timingsWidth[key] = width * frac;
                }
                else {
                    timingsWidth[key] = '0';
                }
            });

            timingsWidth['_lpad'] = pads[id][0] * frac;
            timingsWidth['_rpad'] = pads[id][1] * frac;

            var timeHtml = Mustache.to_html(timingsTemplate, {
                timings: timingsWidth,
                id: id
            });

            var size = 0;

            size = size + getRealSize(entry.response);

            totalRespSize = totalRespSize + size;

            var domain = extractDomain(entry.request.url);
            if (domainRequests[domain] === undefined) {
                domainRequests[domain] = { c: 0, s: 0, t: 0 };
            }
            domainRequests[domain].t = domainRequests[domain].t + total;
            domainRequests[domain].c = domainRequests[domain].c + 1;
            domainRequests[domain].s = domainRequests[domain].s + size;

            html = Mustache.to_html(reqTemplate, {
                id: id,
                time: totals[id],
                request: entry.request,
                response: entry.response,
                timings: entry.timings,
                statusClass: statusClass,
                time: formatTime(entry.total.toFixed()),
                timeline: timeHtml,
                size: formatBytes(size),
                rawSize: size,
                time_title: JSON.stringify(entry.timings)
            });
            //console.log('complete html for ' + id);
            $(html).insertBefore($('#summary', $target));

            var requestHeaders = Mustache.to_html(headersTemplate, {
                headers: entry.request.headers
            });

            var respHeaders = Mustache.to_html(headersTemplate, {
                headers: entry.response.headers
            });

            var disabledTabs = [], query = '';
            if (entry.request.queryString && entry.request.queryString.length > 0) {
                query = Mustache.to_html(headersTemplate, {
                    headers: entry.request.queryString
                })
            }
            else {
                disabledTabs.push(1);
            }
            if (!entry.request.postData || !entry.request.postData.text) {
                disabledTabs.push(2);
            }
            if (!entry.response.content || !entry.response.content.text) {
                disabledTabs.push(3)
            }


            html = Mustache.to_html(detailsTemplate, {
                id: id,
                time: totals[id],
                request: entry.request,
                response: entry.response,
                timings: entry.timings,
                rq_headers: requestHeaders,
                rs_headers: respHeaders,
                disabled_tabs: disabledTabs.join(','),
                query: query,
                statusClass: btnStatus
            });

            processContentType(entry.response, entry.total, size);

            $(html).insertBefore($('#summary', $target));

        }

        function processContentType(response, total, size) {
            var headers = response.headers;
            if (headers) {
                var cTypeHeader = headers.find(function (a) { return a.name === 'content-type' || a.name === 'Content-Type' });

                if (cTypeHeader) {
                    var cTypeVal = cTypeHeader.value;

                    if (cTypeVal.toLowerCase() == 'null' || cTypeVal.toLowerCase() == 'none') {
                        cTypeVal = response.content.mimeType;
                    }

                    var cTypeValShort = cTypeVal.split(';')[0];

                    if (cTypeValShort !== undefined) {
                        var cTypeSplit = cTypeValShort.split('/');
                        var cType = cTypeSplit[0];
                        var cSubType = cTypeSplit[1];
                        var lookupCType;

                        if (cType == 'audio' || cType == 'video' || cType == 'image') {
                            lookupCType = cType;
                        } else if (cType == 'text') {
                            if (cSubType == 'css' || cSubType == 'html' || cSubType == 'javascript') {
                                lookupCType = cSubType;
                            } else {
                                lookupCType = cType;
                            }
                        } else {
                            if (cSubType.startsWith("x-")) {
                                lookupCType = cSubType.substring(2);
                            } else {
                                lookupCType = cSubType;
                            }
                        }

                        if (resources[lookupCType] === undefined) {
                            resources[lookupCType] = { c: 0, s: 0, t: 0 };
                        }
                        resources[lookupCType].c = resources[lookupCType].c + 1;
                        resources[lookupCType].t = resources[lookupCType].t + total;

                        if (response.bodySize) {
                            resources[lookupCType].s = resources[lookupCType].s + size;
                        }
                    }
                }
            }
        }
    };
    $.fn.HarView = function (options) {
        return this.each(function () {
            var element = $(this);

            // pass options to plugin constructor
            var harView = new HarView(this, options);

            // Store plugin object in this element's data
            element.data('HarView', harView);
        });
    };
})(jQuery);

function formatBytes(bytes) {
    if (bytes < 1024) return bytes + " B";
    else if (bytes < 1048576) return (bytes / 1024).toFixed(1) + " KB";
    else if (bytes < 1073741824) return (bytes / 1048576).toFixed(1) + " MB";
    else return (bytes / 1073741824).toFixed(1) + " GB";
};

function formatTime(milliSeconds) {
    if (milliSeconds < 1000) {
        return milliSeconds + " ms";
    } else if (milliSeconds < 60000) {
        return (milliSeconds / 1000).toFixed(1) + " secs";
    }
    else {
        var seconds = milliSeconds / 1000;
        var mins = Math.floor(seconds / 60);
        var secondsRem = Math.round(seconds - mins * 60);

        return mins + ' mins ' + secondsRem + ' secs';
    }
};

function extractDomain(url) {
    var domain;

    //find & remove protocol (http, ftp, etc.) and get domain
    if (url.indexOf("://") > -1) {
        domain = url.split('/')[2];
    }
    else {
        domain = url.split('/')[0];
    }

    //find & remove port number
    domain = domain.split(':')[0];

    return domain;
}

function getRealSize(response) {
    var tmp, size;

    if (response.hasOwnProperty('_transferSize')) {
        return parseInt(response._transferSize, 10);
    }

    if (response.hasOwnProperty('content')) {
        return response.content.size;
    }

    for (var i = 0; i < response.headers.length; i++) {
        tmp = response.headers[i];
        if (tmp.name == 'content-length' || tmp.name == 'Content-Length') {
            return parseInt(tmp.value, 10);
        }
    }

    if (response.bodySize && response.bodySize > 0) {
        return response.bodySize;
    }

    return 0;
}
