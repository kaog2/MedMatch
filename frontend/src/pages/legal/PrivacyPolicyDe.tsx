import { Box, Typography } from '@mui/material';

export default function PrivacyPolicyDe() {
  return (
    <>
      <Typography variant="body1" sx={{ mb: 3 }}>
        Diese Datenschutzerklärung klärt Sie über die Art, den Umfang und den Zweck der Verarbeitung
        von personenbezogenen Daten innerhalb unserer Webanwendung MedMatch (nachfolgend
        «Plattform», «wir» oder «uns») auf. Die Verarbeitung erfolgt unter strikter Einhaltung der
        Bestimmungen der Datenschutz-Grundverordnung (DSGVO) sowie des Bundesdatenschutzgesetzes (BDSG).
      </Typography>

      {/* ── 1. Name und Kontaktdaten des Verantwortlichen ── */}
      <Typography variant="h5">1. Name und Kontaktdaten des Verantwortlichen</Typography>
      <Typography variant="body1">
        Verantwortlicher im Sinne von Art. 4 Nr. 7 DSGVO für die Verarbeitung personenbezogener Daten auf
        dieser Plattform ist:
      </Typography>
      <ul>
        <li><Typography variant="body1"><strong>Plattform:</strong> MedMatch</Typography></li>
        <li><Typography variant="body1"><strong>E-Mail für Datenschutzfragen:</strong> datenschutz@medmatch.app</Typography></li>
        <li><Typography variant="body1"><strong>Allgemeine Rechtsabteilung:</strong> legal@medmatch.app</Typography></li>
        <li><Typography variant="body1"><strong>Website:</strong> https://medmatch.app</Typography></li>
      </ul>

      {/* ── 2. Rechtsgrundlagen und Kategorien erhobener Daten ── */}
      <Typography variant="h5">2. Erhebung und Kategorien personenbezogener Daten</Typography>

      <Typography variant="h6">2.1 Registrierungs- und Bestandsdaten</Typography>
      <Typography variant="body1">
        Im Rahmen der Registrierung und Kontoführung verarbeiten wir folgende Basisdaten:
      </Typography>
      <ul>
        <li><Typography variant="body1">E-Mail-Adresse (zur Identifikation und Kontoverwaltung)</Typography></li>
        <li><Typography variant="body1">Passwort (ausschließlich als kryptografisch gesalzener Einweg-Hash via Argon2/BCrypt gespeichert)</Typography></li>
        <li><Typography variant="body1">Benutzerrolle (Patient, Klinik/Zentrum, Arzt)</Typography></li>
        <li><Typography variant="body1">Zeitpunkt der Registrierung und Login-Metadaten</Typography></li>
      </ul>
      <Typography variant="body1">
        <em>Rechtsgrundlage:</em> Art. 6 Abs. 1 lit. b DSGVO (Erforderlich zur Erfüllung des
        Nutzungsvertrags über die Plattform).
      </Typography>

      {/* ── 3. Besondere Kategorien personenbezogener Daten (Gesundheitsdaten) ── */}
      <Typography variant="h5">3. Verarbeitung von Gesundheitsdaten (Art. 9 DSGVO)</Typography>
      <Box
        sx={{
          bgcolor: 'rgba(0,105,92,.06)',
          border: '1.5px solid #00695c',
          borderRadius: 2,
          p: 2.5,
          my: 2,
        }}
      >
        <Typography variant="body1" sx={{ fontWeight: 700, color: '#00695c', mb: 1 }}>
          Besonderer Schutz sensibler Gesundheitsdaten
        </Typography>
        <Typography variant="body1">
          Angaben über körperliche oder seelische Beschwerden, Diagnosen und Symptome stellen
          <strong> besondere Kategorien personenbezogener Daten gem. Art. 9 Abs. 1 DSGVO
          («Gesundheitsdaten»)</strong> dar. Die Verarbeitung dieser Daten erfolgt <strong>ausschließlich
          auf Grundlage Ihrer ausdrücklichen, freiwilligen und informierten Einwilligung gemäß Art. 9
          Abs. 2 lit. a DSGVO i.V.m. Art. 6 Abs. 1 lit. a DSGVO</strong>.
        </Typography>
      </Box>

      <Typography variant="body1">
        Zu den freiwillig von Ihnen hinterlegten Gesundheitsdaten gehören insbesondere:
      </Typography>
      <ul>
        <li><Typography variant="body1">Vom Nutzer angegebene Diagnosen und Vorerkrankungen</Typography></li>
        <li><Typography variant="body1">Körperliche Schmerzzustände und Beschwerden</Typography></li>
        <li><Typography variant="body1">Einträge im Schmerztagebuch (Symptom Diary): anatomische Schmerzlokalisation,
          Schmerzqualität (z. B. stechend, brennend, pochend, dumpf, ausstrahlend), Schmerzintensität
          (numerische Ratingskala 0 bis 10), Dauer, Auslöser und lindernde Faktoren</Typography></li>
        <li><Typography variant="body1">Medikamenteneinnahme und Therapiemaßnahmen</Typography></li>
        <li><Typography variant="body1">Persönliche Reflexionsnotizen zum täglichen Wohlbefinden und Schlafqualität</Typography></li>
      </ul>

      <Typography variant="h6">Widerrufsrecht für Gesundheitsdaten (Art. 7 Abs. 3 DSGVO)</Typography>
      <Typography variant="body1">
        Sie haben das Recht, Ihre Einwilligung in die Verarbeitung Ihrer Gesundheitsdaten <strong>jederzeit
        ohne Angabe von Gründen mit Wirkung für die Zukunft zu widerrufen</strong>. Durch den Widerruf
        wird die Rechtmäßigkeit der aufgrund der Einwilligung bis zum Widerruf erfolgten Verarbeitung
        nicht berührt. Der Widerruf kann formlos per E-Mail an <strong>datenschutz@medmatch.app</strong> oder
        durch Löschung Ihres Kontos bzw. einzelner Tagebucheinträge in den Profileinstellungen erfolgen.
      </Typography>

      {/* ── 4. Zweckbindung der Verarbeitung ── */}
      <Typography variant="h5">4. Zweckbindung der Verarbeitung (Art. 5 Abs. 1 lit. b DSGVO)</Typography>
      <Typography variant="body1">
        Ihre Daten werden <strong>ausschließlich zu folgenden festgelegten Zwecken</strong> verarbeitet:
      </Typography>
      <ul>
        <li>
          <Typography variant="body1">
            <strong>Peer-Matching:</strong> Automatisierte Berechnung von Übereinstimmungen zwischen
            Nutzerprofilen anhand vergleichbarer Symptome, um Nutzern die Möglichkeit zu geben, sich
            untereinander zu vernetzen und persönliche Erfahrungen auszutauschen.
          </Typography>
        </li>
        <li>
          <Typography variant="body1">
            <strong>Personalisierte Zentrumsempfehlungen:</strong> Algorithmische Orientierungshilfe zur
            Vorschlagung spezialisierter Kliniken, Behandlungszentren oder Fachärzte basierend auf Ihren
            angegebenen Beschwerden.
          </Typography>
        </li>
        <li>
          <Typography variant="body1">
            <strong>Persönliche Verlaufsanalyse:</strong> Visualisierung aggregierter und pseudonymisierter
            Schmerzverläufe exklusiv für Ihren eigenen persönlichen Gebrauch im Schmerztagebuch.
          </Typography>
        </li>
        <li>
          <Typography variant="body1">
            <strong>Optionaler Chatbot-Symptomeintrag:</strong> Möglichkeit der Schmerzerfassung über
            integrierte Chat-Assistenten (via n8n API) unter Verwendung eines geschützten API-Tokens.
          </Typography>
        </li>
      </ul>
      <Typography variant="body1" sx={{ fontWeight: 700, mt: 1 }}>
        MedMatch betreibt <strong>keinen Verkauf und keine kommerzielle Weitergabe von Gesundheitsdaten</strong> an
        Werbetreibende, Versicherungen oder pharmazeutische Unternehmen. Es findet keine automatisierte
        Entscheidungsfindung mit Rechtswirkung für die betroffene Person gemäß Art. 22 DSGVO statt.
      </Typography>

      {/* ── 5. Rechte der betroffenen Person (Betroffenenrechte) ── */}
      <Typography variant="h5">5. Ihre Rechte als betroffene Person nach der DSGVO</Typography>
      <Typography variant="body1">
        Nach den Bestimmungen der DSGVO stehen Ihnen umfassende Betroffenenrechte gegenüber dem
        Verantwortlichen zu:
      </Typography>
      <ul>
        <li>
          <Typography variant="body1">
            <strong>Auskunftsrecht (Art. 15 DSGVO):</strong> Sie haben das Recht, eine Bestätigung darüber
            zu verlangen, ob Sie betreffende personenbezogene Daten verarbeitet werden, sowie Auskunft über
            die Verarbeitungszwecke, Datenkategorien und Empfänger zu erhalten.
          </Typography>
        </li>
        <li>
          <Typography variant="body1">
            <strong>Recht auf Berichtigung (Art. 16 DSGVO):</strong> Sie können die unverzügliche
            Berichtigung unrichtiger oder die Vervollständigung unvollständiger personenbezogener Daten
            verlangen.
          </Typography>
        </li>
        <li>
          <Typography variant="body1">
            <strong>Recht auf Löschung («Recht auf Vergessenwerden», Art. 17 DSGVO):</strong> Sie können die
            Löschung Ihrer personenbezogenen Daten verlangen, sofern kein gesetzlicher Erlaubnistatbestand
            (z. B. steuerliche Aufbewahrungspflichten) entgegensteht.
          </Typography>
        </li>
        <li>
          <Typography variant="body1">
            <strong>Recht auf Einschränkung der Verarbeitung (Art. 18 DSGVO):</strong> Unter bestimmten
            Voraussetzungen können Sie die Einschränkung der Verarbeitung Ihrer Daten verlangen.
          </Typography>
        </li>
        <li>
          <Typography variant="body1">
            <strong>Recht auf Datenübertragbarkeit (Art. 20 DSGVO):</strong> Sie haben das Recht, Ihre
            personenbezogenen Daten in einem strukturierten, gängigen und maschinenlesbaren Format (z. B.
            JSON/CSV) zu erhalten oder die Übermittlung an einen anderen Verantwortlichen zu fordern.
          </Typography>
        </li>
        <li>
          <Typography variant="body1">
            <strong>Widerspruchsrecht (Art. 21 DSGVO):</strong> Sie können aus Gründen, die sich aus Ihrer
            besonderen Situation ergeben, jederzeit gegen die Verarbeitung Widerspruch einlegen, sofern diese
            auf Art. 6 Abs. 1 lit. f DSGVO beruht.
          </Typography>
        </li>
        <li>
          <Typography variant="body1">
            <strong>Beschwerderecht bei einer Aufsichtsbehörde (Art. 77 DSGVO):</strong> Sie haben das
            Recht, sich bei einer zuständigen Datenschutz-Aufsichtsbehörde (insbesondere im EU-Mitgliedstaat
            Ihres gewöhnlichen Aufenthaltsorts oder des Orts des mutmaßlichen Verstoßes) zu beschweren. Für
            Deutschland ist dies z. B. der jeweilige Landesbeauftragte für den Datenschutz Ihres Bundeslandes
            oder der Bundesbeauftragte für den Datenschutz und die Informationsfreiheit (BfDI).
          </Typography>
        </li>
      </ul>

      {/* ── 6. Datensicherheit und technische Schutzmaßnahmen ── */}
      <Typography variant="h5">6. Datensicherheit und Schutzmaßnahmen (Art. 32 DSGVO)</Typography>
      <Typography variant="body1">
        Unter Berücksichtigung des Stands der Technik implementiert MedMatch strenge technische und
        organisatorische Sicherheitsmaßnahmen (TOMs) zum Schutz Ihrer sensiblen Daten:
      </Typography>
      <ul>
        <li><Typography variant="body1">Durchgehende Transportverschlüsselung via TLS 1.3 / HTTPS</Typography></li>
        <li><Typography variant="body1">Verschlüsselung sensibler Datenfelder auf Datenbankebene (Encryption at Rest)</Typography></li>
        <li><Typography variant="body1">Strikte Mandantentrennung und Zugriffsbeschränkungen nach dem Least-Privilege-Prinzip</Typography></li>
        <li><Typography variant="body1">Hashing von API-Schlüsseln mittels kryptografischer SHA-256-Prüfsummen</Typography></li>
        <li><Typography variant="body1">Hosting auf ISO 27001-zertifizierter Serverinfrastruktur innerhalb des europäischen Wirtschaftsraums</Typography></li>
      </ul>

      {/* ── 7. Speicherdauer und Datenlöschung ── */}
      <Typography variant="h5">7. Speicherdauer und Kriterien für die Löschung</Typography>
      <Typography variant="body1">
        Personenbezogene Daten werden nur so lange gespeichert, wie dies für die Zwecke, für die sie
        erhoben wurden, erforderlich ist, oder solange die Einwilligung des Nutzers fortbesteht.
      </Typography>
      <Typography variant="body1" sx={{ mt: 1 }}>
        Bei Kontolöschung durch den Nutzer werden sämtliche persönlichen Profilangaben,
        Symptomtagebucheinträge und Matchings unverzüglich und unwiderruflich aus den operativen Systemen
        gelöscht.
      </Typography>

      {/* ── 8. Cookies und Tracking ── */}
      <Typography variant="h5">8. Cookies und clientseitige Speicherung</Typography>
      <Typography variant="body1">
        MedMatch setzt <strong>ausschließlich technisch zwingend notwendige Session-Tokens</strong> (JWT) und
        lokale Speicherwerte (z. B. Sprachpräferenz via <code>localStorage</code>) ein.
      </Typography>
      <ul>
        <li><Typography variant="body1">Keine Drittanbieter-Werbecookies oder Tracking-Pixel</Typography></li>
        <li><Typography variant="body1">Keine Weitergabe von Benutzerdaten an Werbenetzwerke</Typography></li>
        <li><Typography variant="body1">Verwendung von OpenTelemetry/Alloy ausschließlich für systemische Fehler- und Leistungsdiagnose ohne Personenbezug zu Gesundheitsdaten</Typography></li>
      </ul>

      {/* ── 9. Kontakt ── */}
      <Typography variant="h5">9. Kontaktstelle für Datenschutzfragen</Typography>
      <Typography variant="body1">
        Für Fragen zur Verarbeitung Ihrer personenbezogenen Daten oder zur Ausübung Ihrer Betroffenenrechte
        erreichen Sie unser Datenschutz-Team jederzeit unter:
      </Typography>
      <ul>
        <li><Typography variant="body1"><strong>E-Mail:</strong> datenschutz@medmatch.app</Typography></li>
      </ul>
    </>
  );
}
