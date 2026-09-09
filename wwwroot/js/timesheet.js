(function () {
    'use strict';

    var data = window.tfTimesheetData;
    if (!data) {
        return;
    }

    var container = document.getElementById('timesheet-grid');
    var datalistId = 'tf-clienti-list';

    function testoSicuro(v) {
        return v === null || v === undefined ? '' : String(v);
    }

    function formattaOre(v) {
        if (v === null || v === undefined || v === '') return '';
        return v.toString().replace('.', ',');
    }

    function trovaCliente(nome) {
        return data.clienti.find(function (c) { return c.nome === nome; });
    }

    function trovaProgetto(cliente, nome) {
        if (!cliente) return null;
        return cliente.progetti.find(function (p) { return p.nome === nome; }) || null;
    }

    function costruisciDatalist(id, valori) {
        var dl = document.createElement('datalist');
        dl.id = id;
        valori.forEach(function (v) {
            var opt = document.createElement('option');
            opt.value = v;
            dl.appendChild(opt);
        });
        return dl;
    }

    function aggiornaDatalist(id, valori) {
        var dl = document.getElementById(id);
        if (!dl) return;
        dl.innerHTML = '';
        valori.forEach(function (v) {
            var opt = document.createElement('option');
            opt.value = v;
            dl.appendChild(opt);
        });
    }

    // Datalist globale clienti
    var dlClienti = costruisciDatalist(datalistId, data.clienti.map(function (c) { return c.nome; }));
    document.body.appendChild(dlClienti);

    var table = document.createElement('table');
    table.className = 'tf-grid table table-bordered table-sm';
    table.style.minWidth = (445 + data.giorniNelMese * 34) + 'px';

    var thead = document.createElement('thead');
    var headRow = document.createElement('tr');

    function aggiungiHeaderCell(testo, classi) {
        var th = document.createElement('th');
        th.textContent = testo;
        if (classi) th.className = classi;
        headRow.appendChild(th);
        return th;
    }

    function aggiungiHeaderGiorno(g) {
        var th = document.createElement('th');
        var classi = 'tf-day';
        if (g.festivo) classi += ' tf-festivo';
        else if (g.weekend) classi += ' tf-weekend';
        th.className = classi;

        var nome = document.createElement('span');
        nome.className = 'tf-day-nome';
        nome.textContent = g.nomeGiornoBreve;

        var numero = document.createElement('span');
        numero.className = 'tf-day-numero';
        numero.textContent = g.giorno;

        th.appendChild(nome);
        th.appendChild(numero);
        headRow.appendChild(th);
        return th;
    }

    aggiungiHeaderCell('Cliente');
    aggiungiHeaderCell('Progetto');
    aggiungiHeaderCell('Attività');

    data.giorni.forEach(function (g) {
        aggiungiHeaderGiorno(g);
    });

    aggiungiHeaderCell('Totale', 'tf-totale-col');

    thead.appendChild(headRow);
    table.appendChild(thead);

    var RIGHE_VISIBILI = 15;

    var tbody = document.createElement('tbody');
    table.appendChild(tbody);

    var tfoot = document.createElement('tfoot');
    table.appendChild(tfoot);

    container.innerHTML = '';
    container.appendChild(table);

    var righeDati = data.righe.map(function (r) {
        return {
            progettoId: r.progettoId,
            attivitaId: r.attivitaId,
            clienteNome: r.clienteNome,
            progettoNome: r.progettoNome,
            attivitaNome: r.attivitaNome || '',
            ore: r.ore.slice()
        };
    });

    var contatoreDatalist = 0;

    function creaCellaTesto(valore, opzioni) {
        var td = document.createElement('td');
        var input = document.createElement('input');
        input.type = 'text';
        input.className = 'form-control form-control-sm';
        input.value = testoSicuro(valore);
        if (opzioni && opzioni.datalistId) {
            input.setAttribute('list', opzioni.datalistId);
        }
        td.appendChild(input);
        return { td: td, input: input };
    }

    function creaCellaOre(valore, classi) {
        var td = document.createElement('td');
        if (classi) td.className = classi;
        var input = document.createElement('input');
        input.type = 'text';
        input.inputMode = 'decimal';
        input.className = 'form-control form-control-sm tf-ore';
        input.value = formattaOre(valore);
        td.appendChild(input);
        return { td: td, input: input };
    }

    function calcolaTotaleRiga(rigaEl) {
        var input = rigaEl.querySelectorAll('.tf-ore');
        var totale = 0;
        input.forEach(function (inp) {
            var v = parseFloat((inp.value || '').replace(',', '.'));
            if (!isNaN(v)) totale += v;
        });
        var totaleEl = rigaEl.querySelector('.tf-totale');
        totaleEl.textContent = totale > 0 ? totale.toString().replace('.', ',') : '';
        calcolaTotaliColonne();
    }

    function censisciLocalmente(clienteNome, progettoNome, attivitaNome) {
        var cliente = trovaCliente(clienteNome);
        if (!cliente) {
            cliente = { nome: clienteNome, progetti: [] };
            data.clienti.push(cliente);
            aggiornaDatalist(datalistId, data.clienti.map(function (c) { return c.nome; }));
        }

        var progetto = trovaProgetto(cliente, progettoNome);
        if (!progetto) {
            progetto = { nome: progettoNome, attivita: [] };
            cliente.progetti.push(progetto);
        }

        if (attivitaNome && !progetto.attivita.some(function (a) { return a.nome === attivitaNome; })) {
            progetto.attivita.push({ nome: attivitaNome });
        }
    }

    var codaSalvataggi = Promise.resolve();
    var salvataggiInCorso = 0;
    var erroreSalvataggio = false;
    var statoSalvataggio = document.createElement('div');
    statoSalvataggio.setAttribute('role', 'status');
    statoSalvataggio.hidden = true;
    container.parentNode.insertBefore(statoSalvataggio, container);

    function accodaSalvataggio(operazione) {
        salvataggiInCorso++;
        statoSalvataggio.hidden = false;
        statoSalvataggio.textContent = 'Salvataggio in corso…';
        codaSalvataggi = codaSalvataggi.then(operazione);
        codaSalvataggi.then(function () {
            salvataggiInCorso--;
            statoSalvataggio.hidden = salvataggiInCorso === 0;
        }, function (err) {
            salvataggiInCorso--;
            erroreSalvataggio = true;
            statoSalvataggio.textContent = 'Salvataggio non riuscito. Le modifiche non sono confermate: non cambiare mese. ' + err.message;
            console.error('Errore salvataggio timesheet', err);
        });
    }

    function leggiRisposta(r) {
        if (!r.ok || r.redirected) {
            throw new Error('Risposta del server non valida (' + r.status + ').');
        }
        return r.json().then(function (res) {
            if (!res.ok || !res.progettoId) throw new Error('Conferma del salvataggio mancante.');
            return res;
        });
    }

    function aggiornaIdentita(tr, res) {
        tr.dataset.progettoId = res.progettoId;
        tr.dataset.attivitaId = res.attivitaId == null ? '' : res.attivitaId;
    }

    window.tfNavigaMese = async function (url) {
        if (document.activeElement) document.activeElement.blur();
        try {
            var coda;
            do {
                coda = codaSalvataggi;
                await coda;
            } while (coda !== codaSalvataggi);
            window.location.href = url;
        } catch (_) {
            // L'errore viene mostrato accanto alla griglia.
        }
    };

    window.addEventListener('beforeunload', function (event) {
        if (salvataggiInCorso || erroreSalvataggio) {
            event.preventDefault();
            event.returnValue = '';
        }
    });

    function salvaCella(tr, clienteNome, progettoNome, attivitaNome, giorno, ore) {
        if (!clienteNome || !progettoNome) {
            throw new Error('Specificare cliente e progetto.');
        }

        return fetch(data.salvaCellaUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': ottieniToken()
            },
            body: JSON.stringify({
                anno: data.anno,
                mese: data.mese,
                giorno: giorno,
                clienteNome: clienteNome,
                progettoNome: progettoNome,
                attivitaNome: attivitaNome || null,
                ore: ore
            })
        }).then(leggiRisposta).then(function (res) {
            aggiornaIdentita(tr, res);
            censisciLocalmente(clienteNome, progettoNome, attivitaNome);
        });
    }

    function ottieniToken() {
        var input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    function aggiornaRiga(tr, clienteNome, progettoNome, attivitaNome) {
        if (!tr.dataset.progettoId) {
            return;
        }
        if (!clienteNome || !progettoNome) {
            throw new Error('Specificare cliente e progetto.');
        }

        return fetch(data.aggiornaRigaUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': ottieniToken()
            },
            body: JSON.stringify({
                anno: data.anno,
                mese: data.mese,
                clienteNome: clienteNome,
                progettoNome: progettoNome,
                attivitaNome: attivitaNome || null,
                vecchioProgettoId: parseInt(tr.dataset.progettoId, 10),
                vecchioAttivitaId: tr.dataset.attivitaId ? parseInt(tr.dataset.attivitaId, 10) : null
            })
        }).then(leggiRisposta).then(function (res) {
            aggiornaIdentita(tr, res);
            censisciLocalmente(clienteNome, progettoNome, attivitaNome);
        });
    }

    function aggiornaColoreRiga(tr, colore) {
        tr.querySelectorAll('td').forEach(function (td) {
            if (td.classList.contains('tf-weekend') || td.classList.contains('tf-festivo') || td.classList.contains('tf-totale')) {
                return;
            }
            td.style.backgroundColor = colore || '';
        });
    }

    function aggiungiRiga(riga) {
        var tr = document.createElement('tr');
        tr.dataset.progettoId = riga.progettoId || '';
        tr.dataset.attivitaId = riga.attivitaId === null || riga.attivitaId === undefined ? '' : riga.attivitaId;

        var progIdList = 'tf-progetti-' + (contatoreDatalist++);
        var attIdList = 'tf-attivita-' + (contatoreDatalist++);

        var celClienti = creaCellaTesto(riga.clienteNome, { datalistId: datalistId });
        var celProgetto = creaCellaTesto(riga.progettoNome, { datalistId: progIdList });
        var celAttivita = creaCellaTesto(riga.attivitaNome, { datalistId: attIdList });

        var clienteIniziale = trovaCliente(riga.clienteNome);
        var progettoIniziale = trovaProgetto(clienteIniziale, riga.progettoNome);

        var dlProg = costruisciDatalist(progIdList, clienteIniziale ? clienteIniziale.progetti.map(function (p) { return p.nome; }) : []);
        var dlAtt = costruisciDatalist(attIdList, progettoIniziale ? progettoIniziale.attivita.map(function (a) { return a.nome; }) : []);
        document.body.appendChild(dlProg);
        document.body.appendChild(dlAtt);

        tr.appendChild(celClienti.td);
        tr.appendChild(celProgetto.td);
        tr.appendChild(celAttivita.td);

        function salvaIntestazione() {
            var clienteNome = celClienti.input.value.trim();
            var progettoNome = celProgetto.input.value.trim();
            var attivitaNome = celAttivita.input.value.trim();
            if (!clienteNome || !progettoNome) return;
            accodaSalvataggio(function () {
                return aggiornaRiga(tr, clienteNome, progettoNome, attivitaNome);
            });
        }

        celClienti.input.addEventListener('change', function () {
            var cliente = trovaCliente(celClienti.input.value.trim());
            aggiornaDatalist(progIdList, cliente ? cliente.progetti.map(function (p) { return p.nome; }) : []);
            celProgetto.input.value = '';
            celAttivita.input.value = '';
            aggiornaDatalist(attIdList, []);
            aggiornaColoreRiga(tr, cliente ? cliente.colore : null);
        });

        celProgetto.input.addEventListener('change', function () {
            var cliente = trovaCliente(celClienti.input.value.trim());
            var progetto = trovaProgetto(cliente, celProgetto.input.value.trim());
            aggiornaDatalist(attIdList, progetto ? progetto.attivita.map(function (a) { return a.nome; }) : []);

            salvaIntestazione();
        });

        celAttivita.input.addEventListener('change', salvaIntestazione);

        data.giorni.forEach(function (g, indice) {
            var classi = 'tf-day-cell';
            if (g.festivo) classi += ' tf-festivo';
            else if (g.weekend) classi += ' tf-weekend';

            var cellaOre = creaCellaOre(riga.ore[indice], classi);
            tr.appendChild(cellaOre.td);

            cellaOre.input.addEventListener('change', function () {
                calcolaTotaleRiga(tr);

                var clienteNome = celClienti.input.value.trim();
                var progettoNome = celProgetto.input.value.trim();
                var attivitaNome = celAttivita.input.value.trim();
                var valoreGrezzo = cellaOre.input.value.trim();
                var ore = valoreGrezzo ? parseFloat(valoreGrezzo.replace(',', '.')) : null;

                if (ore !== null && isNaN(ore)) {
                    cellaOre.input.value = '';
                    return;
                }

                accodaSalvataggio(function () {
                    return salvaCella(tr, clienteNome, progettoNome, attivitaNome, indice + 1, ore);
                });
            });
        });

        var totaleTd = document.createElement('td');
        totaleTd.className = 'tf-totale text-center';
        tr.appendChild(totaleTd);

        tbody.appendChild(tr);
        calcolaTotaleRiga(tr);
        aggiornaColoreRiga(tr, clienteIniziale ? clienteIniziale.colore : null);
    }

    function calcolaTotaliColonne() {
        var totaliGiorni = new Array(data.giorniNelMese).fill(0);
        var totaleGenerale = 0;

        tbody.querySelectorAll('tr').forEach(function (tr) {
            var inputOre = tr.querySelectorAll('.tf-ore');
            inputOre.forEach(function (inp, indice) {
                var v = parseFloat((inp.value || '').replace(',', '.'));
                if (!isNaN(v)) {
                    totaliGiorni[indice] += v;
                    totaleGenerale += v;
                }
            });
        });

        var celleTotale = tfoot.querySelectorAll('.tf-totale-colonna');
        celleTotale.forEach(function (td, indice) {
            var v = totaliGiorni[indice];
            td.textContent = v > 0 ? v.toString().replace('.', ',') : '';
        });

        var totaleGeneraleEl = tfoot.querySelector('.tf-totale-generale');
        if (totaleGeneraleEl) {
            totaleGeneraleEl.textContent = totaleGenerale > 0 ? totaleGenerale.toString().replace('.', ',') : '';
        }

        var giorniEl = tfoot.querySelector('.tf-footer-giorni');
        if (giorniEl) {
            var giornate = totaleGenerale / 8;
            giornate = Math.round(giornate * 1000) / 1000;
            giorniEl.textContent = 'Giorni: ' + giornate.toString().replace('.', ',');
        }
    }

    function costruisciFooter() {
        var tr = document.createElement('tr');
        tr.className = 'tf-footer-riga';

        var tdComandi = document.createElement('td');
        tdComandi.colSpan = 2;
        tdComandi.className = 'tf-footer-comandi';

        var btnAggiungi = document.createElement('button');
        btnAggiungi.type = 'button';
        btnAggiungi.className = 'btn btn-sm btn-outline-primary me-1';
        btnAggiungi.textContent = 'Aggiungi riga';
        btnAggiungi.addEventListener('click', function () {
            aggiungiRiga(rigaVuota());
        });

        var btnEsportaMese = document.createElement('button');
        btnEsportaMese.type = 'button';
        btnEsportaMese.className = 'btn btn-sm btn-outline-secondary me-1';
        btnEsportaMese.textContent = 'Esporta mese';
        btnEsportaMese.addEventListener('click', function () {
            window.location.href = data.esportaMeseUrl;
        });

        var btnEsportaCliente = document.createElement('button');
        btnEsportaCliente.type = 'button';
        btnEsportaCliente.className = 'btn btn-sm btn-outline-secondary';
        btnEsportaCliente.textContent = 'Esporta per cliente';
        btnEsportaCliente.addEventListener('click', function () {
            apriModalEsportaCliente();
        });

        tdComandi.appendChild(btnAggiungi);
        tdComandi.appendChild(btnEsportaMese);
        tdComandi.appendChild(btnEsportaCliente);
        tr.appendChild(tdComandi);

        var tdEtichettaTotale = document.createElement('td');
        tdEtichettaTotale.className = 'tf-footer-etichetta text-end';

        var spanGiorni = document.createElement('span');
        spanGiorni.className = 'tf-footer-giorni';
        spanGiorni.textContent = 'Giorni: 0';
        tdEtichettaTotale.appendChild(spanGiorni);
        tdEtichettaTotale.appendChild(document.createTextNode('Totale'));
        tr.appendChild(tdEtichettaTotale);

        data.giorni.forEach(function () {
            var td = document.createElement('td');
            td.className = 'tf-totale-colonna text-center';
            tr.appendChild(td);
        });

        var tdTotaleGenerale = document.createElement('td');
        tdTotaleGenerale.className = 'tf-totale-generale text-center';
        tr.appendChild(tdTotaleGenerale);

        tfoot.appendChild(tr);
    }

    function rigaVuota() {
        return {
            clienteNome: '',
            progettoNome: '',
            attivitaNome: '',
            ore: new Array(data.giorniNelMese).fill(null)
        };
    }

    function riempiSpazioDisponibile() {
        while (tbody.rows.length < RIGHE_VISIBILI) {
            aggiungiRiga(rigaVuota());
        }
    }

    function apriModalEsportaCliente() {
        var select = document.getElementById('tf-esporta-cliente-select');
        if (select && select.options.length === 0) {
            data.clienti
                .slice()
                .sort(function (a, b) { return a.nome.localeCompare(b.nome); })
                .forEach(function (c) {
                    var opt = document.createElement('option');
                    opt.value = c.id;
                    opt.textContent = c.nome;
                    select.appendChild(opt);
                });
        }

        var modalEl = document.getElementById('tf-modal-esporta-cliente');
        var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();
    }

    var btnConfermaEsportaCliente = document.getElementById('tf-esporta-cliente-conferma');
    if (btnConfermaEsportaCliente) {
        btnConfermaEsportaCliente.addEventListener('click', function () {
            var select = document.getElementById('tf-esporta-cliente-select');
            var clienteId = select && select.value;
            if (!clienteId) {
                return;
            }
            var annoIntero = document.getElementById('tf-esporta-cliente-anno').checked;
            var separatore = data.esportaClienteUrlBase.indexOf('?') === -1 ? '?' : '&';
            var url = data.esportaClienteUrlBase
                + separatore + 'clienteId=' + encodeURIComponent(clienteId)
                + '&anno=' + encodeURIComponent(data.anno)
                + '&mese=' + encodeURIComponent(data.mese)
                + '&annoIntero=' + (annoIntero ? 'true' : 'false');
            window.location.href = url;

            var modalEl = document.getElementById('tf-modal-esporta-cliente');
            var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
            modal.hide();
        });
    }

    righeDati.forEach(aggiungiRiga);
    riempiSpazioDisponibile();
    costruisciFooter();
    calcolaTotaliColonne();

    tbody.addEventListener('input', function (event) {
        if (event.target.closest('tr') === tbody.lastElementChild && tbody.rows.length < RIGHE_VISIBILI) {
            aggiungiRiga(rigaVuota());
        }
    });
})();
