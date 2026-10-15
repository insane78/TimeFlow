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

    function formattaNumero(v, decimali) {
        return v.toLocaleString('it-IT', { minimumFractionDigits: decimali, maximumFractionDigits: decimali });
    }

    function valoreMeseCorrente() {
        var mese = data.mese < 10 ? '0' + data.mese : String(data.mese);
        return data.anno + '-' + mese;
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
    table.style.minWidth = (510 + data.giorniNelMese * 34) + 'px';

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
        if (g.oggi) classi += ' tf-oggi';
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
    aggiungiHeaderCell('Giorni', 'tf-giorni-col');

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
        var giorniEl = rigaEl.querySelector('.tf-giorni-riga');
        if (giorniEl) {
            var giorni = Math.round((totale / 8) * 1000) / 1000;
            giorniEl.textContent = giorni !== 0 ? formattaNumero(giorni, 3) : '';
        }
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
            if (td.classList.contains('tf-weekend') || td.classList.contains('tf-festivo') || td.classList.contains('tf-totale') || td.classList.contains('tf-giorni-riga')) {
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
            if (g.oggi) classi += ' tf-oggi';

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
        var giorniTd = document.createElement('td');
        giorniTd.className = 'tf-giorni-riga text-center';
        tr.appendChild(giorniTd);

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

        var giorniGeneraliEl = tfoot.querySelector('.tf-giorni-generale');
        if (giorniGeneraliEl) {
            var giorniGenerali = Math.round((totaleGenerale / 8) * 1000) / 1000;
            giorniGeneraliEl.textContent = giorniGenerali !== 0 ? formattaNumero(giorniGenerali, 3) : '';
        }

        var giorniValoreEl = tfoot.querySelector('.tf-footer-giorni-valore');
        if (giorniValoreEl) {
            var giornate = totaleGenerale / 8;
            giornate = Math.round(giornate * 1000) / 1000;
            giorniValoreEl.textContent = giornate !== 0 ? formattaNumero(giornate, 3) : '';

            var importoValoreEl = tfoot.querySelector('.tf-footer-importo-valore');
            if (importoValoreEl) {
                if (typeof data.compensoGiornaliero === 'number') {
                    var importo = Math.round(giornate * data.compensoGiornaliero * 100) / 100;
                    importoValoreEl.textContent = importo.toLocaleString('it-IT', { minimumFractionDigits: 0, maximumFractionDigits: 2 }) + '€';
                } else {
                    importoValoreEl.textContent = '';
                }
            }
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
        btnAggiungi.className = 'btn btn-sm btn-outline-primary me-1 tf-icon-btn';
        btnAggiungi.title = 'Aggiungi riga';
        btnAggiungi.setAttribute('aria-label', 'Aggiungi riga');
        btnAggiungi.innerHTML = '<svg class="tf-btn-icon-svg" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true"><path d="M8 2a.5.5 0 0 1 .5.5v5h5a.5.5 0 0 1 0 1h-5v5a.5.5 0 0 1-1 0v-5h-5a.5.5 0 0 1 0-1h5v-5A.5.5 0 0 1 8 2Z"/></svg>';
        btnAggiungi.addEventListener('click', function () {
            aggiungiRiga(rigaVuota());
        });

        var btnEsportaMese = document.createElement('button');
        btnEsportaMese.type = 'button';
        btnEsportaMese.className = 'btn btn-sm btn-outline-secondary me-1 tf-icon-btn';
        btnEsportaMese.title = 'Esporta mese';
        btnEsportaMese.setAttribute('aria-label', 'Esporta mese');
        btnEsportaMese.innerHTML = '<svg class="tf-btn-icon-svg" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true"><path d="M.5 9.9a.5.5 0 0 1 .5.5v2.5a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1v-2.5a.5.5 0 0 1 1 0v2.5a2 2 0 0 1-2 2H2a2 2 0 0 1-2-2v-2.5a.5.5 0 0 1 .5-.5Z"/><path d="M7.646 1.146a.5.5 0 0 1 .708 0l2.5 2.5a.5.5 0 0 1-.708.708L8.5 2.707V10.5a.5.5 0 0 1-1 0V2.707L5.854 4.354a.5.5 0 1 1-.708-.708l2.5-2.5Z"/></svg>';
        btnEsportaMese.addEventListener('click', function () {
            window.location.href = data.esportaMeseUrl;
        });

        var btnEsportaCliente = document.createElement('button');
        btnEsportaCliente.type = 'button';
        btnEsportaCliente.className = 'btn btn-sm btn-outline-secondary tf-icon-btn';
        btnEsportaCliente.title = 'Esporta per cliente';
        btnEsportaCliente.setAttribute('aria-label', 'Esporta per cliente');
        btnEsportaCliente.innerHTML = '<svg class="tf-btn-icon-svg" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true"><path d="M4 1a1 1 0 0 0-1 1v12l2.8-1.4a.5.5 0 0 1 .4 0L9 14V2a1 1 0 0 0-1-1H4Zm5 1.2V14l2.8-1.4a.5.5 0 0 1 .4 0L15 14V2a1 1 0 0 0-1-1h-4a1 1 0 0 0-1 1.2Z"/></svg>';
        btnEsportaCliente.addEventListener('click', function () {
            apriModalEsportaCliente();
        });

        var btnRapportoProgetti = document.createElement('button');
        btnRapportoProgetti.type = 'button';
        btnRapportoProgetti.className = 'btn btn-sm btn-outline-secondary tf-icon-btn';
        btnRapportoProgetti.title = 'Rapporto giorni per progetto';
        btnRapportoProgetti.setAttribute('aria-label', 'Rapporto giorni per progetto');
        btnRapportoProgetti.innerHTML = '<svg class="tf-btn-icon-svg" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true"><path d="M0 0h1v15h15v1H0V0Zm14 2h1v12H3v-1h11V2ZM4 9h2v4H4V9Zm3-3h2v7H7V6Zm3 2h2v5h-2V8Z"/></svg>';
        btnRapportoProgetti.addEventListener('click', function () {
            apriModalRapportoProgetti();
        });

        tdComandi.appendChild(btnAggiungi);
        tdComandi.appendChild(btnEsportaMese);
        tdComandi.appendChild(btnEsportaCliente);
        tdComandi.appendChild(btnRapportoProgetti);
        tr.appendChild(tdComandi);

        var tdEtichettaTotale = document.createElement('td');
        tdEtichettaTotale.className = 'tf-footer-etichetta';

        var miniTable = document.createElement('table');
        miniTable.className = 'tf-footer-mini';

        var rigaGiorni = document.createElement('tr');
        var labelGiorni = document.createElement('td');
        labelGiorni.className = 'tf-footer-mini-label';
        labelGiorni.textContent = 'Giorni:';
        var valoreGiorni = document.createElement('td');
        valoreGiorni.className = 'tf-footer-mini-value tf-footer-giorni-valore';
        valoreGiorni.textContent = '0';
        var mostraImporto = typeof data.compensoGiornaliero === 'number';

        var celTotale = document.createElement('td');
        celTotale.className = 'tf-footer-mini-totale';
        celTotale.rowSpan = mostraImporto ? 2 : 1;
        celTotale.textContent = 'Totale';
        rigaGiorni.appendChild(labelGiorni);
        rigaGiorni.appendChild(valoreGiorni);
        rigaGiorni.appendChild(celTotale);

        var rigaImporto = null;
        if (mostraImporto) {
            rigaImporto = document.createElement('tr');
            var labelImporto = document.createElement('td');
            labelImporto.className = 'tf-footer-mini-label';
            labelImporto.textContent = 'Importo:';
            var valoreImporto = document.createElement('td');
            valoreImporto.className = 'tf-footer-mini-value tf-footer-importo-valore';
            rigaImporto.appendChild(labelImporto);
            rigaImporto.appendChild(valoreImporto);
        }

        miniTable.appendChild(rigaGiorni);
        if (rigaImporto) {
            miniTable.appendChild(rigaImporto);
        }
        tdEtichettaTotale.appendChild(miniTable);
        tr.appendChild(tdEtichettaTotale);

        data.giorni.forEach(function () {
            var td = document.createElement('td');
            td.className = 'tf-totale-colonna text-center';
            tr.appendChild(td);
        });

        var tdTotaleGenerale = document.createElement('td');
        tdTotaleGenerale.className = 'tf-totale-generale text-center';
        tr.appendChild(tdTotaleGenerale);
        var tdGiorniGenerali = document.createElement('td');
        tdGiorniGenerali.className = 'tf-giorni-generale text-center';
        tr.appendChild(tdGiorniGenerali);

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

    function mostraErroreRapporto(testo) {
        var erroreEl = document.getElementById('tf-rapporto-errore');
        if (!erroreEl) {
            return;
        }
        if (!testo) {
            erroreEl.className = 'mt-3 d-none';
            erroreEl.textContent = '';
            return;
        }
        erroreEl.className = 'mt-3 alert alert-danger mb-0';
        erroreEl.textContent = testo;
    }

    function formattaPeriodoRapporto() {
        var modalita = document.querySelector('input[name="tf-rapporto-modalita"]:checked');
        return modalita && modalita.value === 'attivita' ? 'selezione attività' : 'periodo selezionato';
    }

    function renderRigheRapporto(righe) {
        var tbodyRapporto = document.getElementById('tf-rapporto-righe');
        if (!tbodyRapporto) {
            return;
        }
        tbodyRapporto.innerHTML = '';

        if (!righe || righe.length === 0) {
            var trVuota = document.createElement('tr');
            trVuota.innerHTML = '<td colspan="4" class="text-center text-muted">Nessun dato per ' + formattaPeriodoRapporto() + '.</td>';
            tbodyRapporto.appendChild(trVuota);
            return;
        }

        righe.forEach(function (riga) {
            var tr = document.createElement('tr');
            var tdCliente = document.createElement('td');
            tdCliente.textContent = riga.clienteNome;
            var tdProgetto = document.createElement('td');
            tdProgetto.textContent = riga.progettoNome;
            var tdOre = document.createElement('td');
            tdOre.className = 'text-end';
            tdOre.textContent = formattaNumero(riga.totaleOre, 2);
            var tdGiorni = document.createElement('td');
            tdGiorni.className = 'text-end';
            tdGiorni.textContent = formattaNumero(riga.totaleGiorni, 3);

            tr.appendChild(tdCliente);
            tr.appendChild(tdProgetto);
            tr.appendChild(tdOre);
            tr.appendChild(tdGiorni);
            tbodyRapporto.appendChild(tr);
        });
    }

    function svuotaSelect(selectEl, placeholder) {
        if (!selectEl) {
            return;
        }
        selectEl.innerHTML = '';
        var opt = document.createElement('option');
        opt.value = '';
        opt.textContent = placeholder;
        selectEl.appendChild(opt);
    }

    function popolaSelectClienteRapporto() {
        var clienteEl = document.getElementById('tf-rapporto-cliente');
        if (!clienteEl) {
            return;
        }

        svuotaSelect(clienteEl, 'Seleziona cliente');
        data.clienti
            .slice()
            .sort(function (a, b) { return a.nome.localeCompare(b.nome); })
            .forEach(function (cliente) {
                var opt = document.createElement('option');
                opt.value = String(cliente.id);
                opt.textContent = cliente.nome;
                clienteEl.appendChild(opt);
            });
    }

    function popolaSelectProgettoRapporto(clienteId) {
        var progettoEl = document.getElementById('tf-rapporto-progetto');
        if (!progettoEl) {
            return;
        }

        svuotaSelect(progettoEl, 'Seleziona progetto');
        svuotaSelect(document.getElementById('tf-rapporto-attivita'), 'Seleziona attività');
        if (!clienteId) {
            return;
        }

        var cliente = data.clienti.find(function (c) { return String(c.id) === String(clienteId); });
        if (!cliente) {
            return;
        }

        cliente.progetti
            .slice()
            .sort(function (a, b) { return a.nome.localeCompare(b.nome); })
            .forEach(function (progetto) {
                var opt = document.createElement('option');
                opt.value = String(progetto.id);
                opt.textContent = progetto.nome;
                progettoEl.appendChild(opt);
            });
    }

    function popolaSelectAttivitaRapporto(clienteId, progettoId) {
        var attivitaEl = document.getElementById('tf-rapporto-attivita');
        if (!attivitaEl) {
            return;
        }

        svuotaSelect(attivitaEl, 'Seleziona attività');
        if (!clienteId || !progettoId) {
            return;
        }

        var cliente = data.clienti.find(function (c) { return String(c.id) === String(clienteId); });
        if (!cliente) {
            return;
        }
        var progetto = cliente.progetti.find(function (p) { return String(p.id) === String(progettoId); });
        if (!progetto) {
            return;
        }

        progetto.attivita
            .slice()
            .sort(function (a, b) { return a.nome.localeCompare(b.nome); })
            .forEach(function (attivita) {
                var opt = document.createElement('option');
                opt.value = String(attivita.id);
                opt.textContent = attivita.nome;
                attivitaEl.appendChild(opt);
            });
    }

    function aggiornaSezioniRapporto() {
        var modalita = document.querySelector('input[name="tf-rapporto-modalita"]:checked');
        var periodoEl = document.getElementById('tf-rapporto-sezione-periodo');
        var attivitaEl = document.getElementById('tf-rapporto-sezione-attivita');
        if (!periodoEl || !attivitaEl || !modalita) {
            return;
        }

        if (modalita.value === 'attivita') {
            periodoEl.classList.add('d-none');
            attivitaEl.classList.remove('d-none');
        } else {
            periodoEl.classList.remove('d-none');
            attivitaEl.classList.add('d-none');
        }
    }

    function apriModalRapportoProgetti() {
        var inizioEl = document.getElementById('tf-rapporto-inizio');
        var fineEl = document.getElementById('tf-rapporto-fine');
        var clienteEl = document.getElementById('tf-rapporto-cliente');
        var progettoEl = document.getElementById('tf-rapporto-progetto');
        var attivitaEl = document.getElementById('tf-rapporto-attivita');
        var meseCorrente = valoreMeseCorrente();
        if (inizioEl && !inizioEl.value) {
            inizioEl.value = meseCorrente;
        }
        if (fineEl && !fineEl.value) {
            fineEl.value = meseCorrente;
        }
        if (clienteEl && clienteEl.options.length === 0) {
            popolaSelectClienteRapporto();
        }
        if (progettoEl && progettoEl.options.length === 0) {
            svuotaSelect(progettoEl, 'Seleziona progetto');
        }
        if (attivitaEl && attivitaEl.options.length === 0) {
            svuotaSelect(attivitaEl, 'Seleziona attività');
        }

        mostraErroreRapporto('');
        aggiornaSezioniRapporto();

        var modalEl = document.getElementById('tf-modal-rapporto-progetti');
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

    var btnCalcolaRapporto = document.getElementById('tf-rapporto-calcola');
    var clienteRapportoEl = document.getElementById('tf-rapporto-cliente');
    var progettoRapportoEl = document.getElementById('tf-rapporto-progetto');
    var radioModalitaRapporto = document.querySelectorAll('input[name="tf-rapporto-modalita"]');
    if (clienteRapportoEl) {
        clienteRapportoEl.addEventListener('change', function () {
            popolaSelectProgettoRapporto(clienteRapportoEl.value);
        });
    }
    if (progettoRapportoEl) {
        progettoRapportoEl.addEventListener('change', function () {
            var clienteId = clienteRapportoEl ? clienteRapportoEl.value : '';
            popolaSelectAttivitaRapporto(clienteId, progettoRapportoEl.value);
        });
    }
    radioModalitaRapporto.forEach(function (radio) {
        radio.addEventListener('change', aggiornaSezioniRapporto);
    });

    if (btnCalcolaRapporto) {
        btnCalcolaRapporto.addEventListener('click', function () {
            mostraErroreRapporto('');
            var modalita = document.querySelector('input[name="tf-rapporto-modalita"]:checked');
            if (!modalita) {
                mostraErroreRapporto('Modalità rapporto non valida.');
                return;
            }

            var url = '';
            if (modalita.value === 'attivita') {
                var clienteId = clienteRapportoEl ? clienteRapportoEl.value : '';
                var progettoId = progettoRapportoEl ? progettoRapportoEl.value : '';
                var attivitaRapportoEl = document.getElementById('tf-rapporto-attivita');
                var attivitaId = attivitaRapportoEl ? attivitaRapportoEl.value : '';
                if (!clienteId || !progettoId) {
                    mostraErroreRapporto('Seleziona cliente e progetto.');
                    return;
                }

                var separatoreAttivita = data.rapportoAttivitaStoricoUrlBase.indexOf('?') === -1 ? '?' : '&';
                url = data.rapportoAttivitaStoricoUrlBase
                    + separatoreAttivita + 'clienteId=' + encodeURIComponent(clienteId)
                    + '&progettoId=' + encodeURIComponent(progettoId);
                if (attivitaId) {
                    url += '&attivitaId=' + encodeURIComponent(attivitaId);
                }
            } else {
                var inizioEl = document.getElementById('tf-rapporto-inizio');
                var fineEl = document.getElementById('tf-rapporto-fine');
                var inizioValore = inizioEl ? inizioEl.value : '';
                var fineValore = fineEl ? fineEl.value : '';

                if (!inizioValore || !fineValore) {
                    mostraErroreRapporto('Seleziona mese/anno di inizio e fine.');
                    return;
                }

                if (inizioValore > fineValore) {
                    mostraErroreRapporto('Il periodo iniziale non può essere successivo a quello finale.');
                    return;
                }

                var inizioParti = inizioValore.split('-');
                var fineParti = fineValore.split('-');
                var annoInizio = parseInt(inizioParti[0], 10);
                var meseInizio = parseInt(inizioParti[1], 10);
                var annoFine = parseInt(fineParti[0], 10);
                var meseFine = parseInt(fineParti[1], 10);
                var separatorePeriodo = data.rapportoGiorniProgettoUrlBase.indexOf('?') === -1 ? '?' : '&';
                url = data.rapportoGiorniProgettoUrlBase
                    + separatorePeriodo + 'annoInizio=' + encodeURIComponent(annoInizio)
                    + '&meseInizio=' + encodeURIComponent(meseInizio)
                    + '&annoFine=' + encodeURIComponent(annoFine)
                    + '&meseFine=' + encodeURIComponent(meseFine);
            }

            btnCalcolaRapporto.disabled = true;
            fetch(url, { method: 'GET' })
                .then(function (response) {
                    if (!response.ok) {
                        throw new Error('Impossibile calcolare il rapporto.');
                    }
                    return response.json();
                })
                .then(function (res) {
                    if (!res.ok) {
                        throw new Error(res.errore || 'Errore durante il calcolo del rapporto.');
                    }
                    renderRigheRapporto(res.righe);
                })
                .catch(function (err) {
                    mostraErroreRapporto(err.message);
                })
                .finally(function () {
                    btnCalcolaRapporto.disabled = false;
                });
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
