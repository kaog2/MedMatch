import { Box, Typography } from '@mui/material';

export default function TermsOfUseDe() {
  return (
    <>
      {/* ── 1. Geltungsbereich und Vertragsgegenstand ── */}
      <Typography variant="h5">1. Geltungsbereich und Vertragspartner</Typography>
      <Typography variant="body1">
        Diese Allgemeinen Nutzungsbedingungen (nachfolgend «Nutzungsbedingungen») gelten für die
        Nutzung der Plattform MedMatch (nachfolgend «Plattform» oder «wir») durch registrierte sowie
        nicht-registrierte Nutzer.
      </Typography>
      <Typography variant="body1" sx={{ mt: 1.5 }}>
        Mit der Registrierung eines Benutzerkontos oder der fortgesetzten Nutzung unseres Webangebots
        erklärt sich der Nutzer mit der Geltung dieser Nutzungsbedingungen vollumfänglich einverstanden.
      </Typography>

      {/* ── 2. Leistungsbeschreibung der Plattform ── */}
      <Typography variant="h5">2. Leistungsbeschreibung und Zweck der Plattform</Typography>
      <Typography variant="body1">
        MedMatch ist eine digitale Kommunikations- und Orientierungsplattform im Gesundheitsbereich,
        die folgende Kernfunktionen bereitstellt:
      </Typography>
      <ul>
        <li>
          <Typography variant="body1">
            <strong>Peer-Vernetzung:</strong> Nutzer mit ähnlichen gesundheitlichen Beschwerden oder
            Diagnosen können einander vorgeschlagen werden, um sich auf freiwilliger Basis über persönliche
            Erfahrungen auszutauschen.
          </Typography>
        </li>
        <li>
          <Typography variant="body1">
            <strong>Orientierungshilfe & Zentrumsempfehlungen:</strong> Algorithmische Zusammenstellung von
            öffentlich verfügbaren Informationen über spezialisierte medizinische Versorgungszentren,
            Kliniken und Fachärzte.
          </Typography>
        </li>
        <li>
          <Typography variant="body1">
            <strong>Persönliches Symptomtagebuch:</strong> Digitales Schmerztagebuch zur privaten
            Selbstbeobachtung, Dokumentation von Schmerzintensität und Visualisierung persönlicher Tendenzen.
          </Typography>
        </li>
        <li>
          <Typography variant="body1">
            <strong>Optionale Chatbot-Schnittstelle:</strong> Möglichkeit zur strukturierten Eingabe von
            Tagebucheinträgen über konfigurierte Chat-Assistenten.
          </Typography>
        </li>
      </ul>

      {/* ── 3. WICHTIGER MEDIZINISCHER HAFTUNGSAUSSCHLUSS (MEDICAL DISCLAIMER) ── */}
      <Typography variant="h5">3. Wichtiger Medizinischer Haftungsausschluss</Typography>
      <Box
        sx={{
          bgcolor: 'rgba(176,111,66,.08)',
          border: '2px solid #b06f42',
          borderRadius: 2,
          p: 3,
          my: 2,
        }}
      >
        <Typography variant="body1" sx={{ fontWeight: 800, color: '#b06f42', fontSize: '1.05rem', mb: 1.5 }}>
          ⚠ RECHTLICHER MEDIZIN-HINWEIS — BITTE AUFMERKSAM LESEN
        </Typography>
        <Typography variant="body1" sx={{ fontWeight: 700 }}>
          MedMatch ist KEIN medizinischer Leistungserbringer und KEINE ärztliche Praxis. MedMatch erbringt
          keine ärztlichen, heilkundlichen, psychotherapeutischen oder telemedizinischen Leistungen.
        </Typography>
        <Typography variant="body1" sx={{ mt: 1.5 }}>
          Weder die algorithmischen Vorschläge, noch die Vernetzung mit anderen Betroffenen, noch die
          Einträge im Schmerztagebuch stellen eine ärztliche Beratung, Ferndiagnose oder
          Therapieempfehlung dar. Sie <strong>ersetzen keinesfalls die persönliche Konsultation, Diagnose,
          Beratung oder Behandlung durch approbierte Ärzte</strong> oder qualifizierte Angehörige der
          Heilberufe.
        </Typography>
        <Typography variant="body1" sx={{ mt: 1.5 }}>
          Treffen Sie medizinische Entscheidungen niemals eigenmächtig auf Grundlage der auf MedMatch
          gefundenen Informationen. Setzen Sie verordnete Medikamente oder Behandlungen niemals ohne
          Rücksprache mit Ihrem behandelnden Arzt ab oder verändern Sie deren Dosierung.
        </Typography>
        <Typography variant="body1" sx={{ mt: 1.5, fontWeight: 800, color: '#b06f42' }}>
          AKUTE NOTFÄLLE: MedMatch ist NICHT für akute Schmerzzustände, Notfälle oder lebensbedrohliche
          Situationen geeignet. Wenden Sie sich in Notfällen unverzüglich an den Rettungsdienst (bundesweiter
          Notruf 112) oder den ärztlichen Bereitschaftsdienst (Telefon 116 117).
        </Typography>
      </Box>

      {/* ── 4. Haftungsausschluss für Empfehlungen (Kein Behandlungsvertrag) ── */}
      <Typography variant="h5">4. Haftungsausschluss für empfohlene Zentren (Kein Behandlungsvertrag)</Typography>
      <Typography variant="body1">
        MedMatch fungiert ausschließlich als vermittelnde und hinweisgebende Kommunikationsinfrastruktur.
        Daher gilt ausdrücklich:
      </Typography>
      <ul>
        <li>
          <Typography variant="body1">
            <strong>Kein Behandlungsvertrag mit MedMatch:</strong> Ein medizinischer Behandlungsvertrag
            gemäß §§ 630a ff. BGB kommt im Falle einer Inanspruchnahme empfohlener Zentren, Kliniken oder
            Ärzte <strong>ausschließlich und unmittelbar zwischen dem Nutzer und dem jeweiligen
            Leistungserbringer</strong> zustande. MedMatch ist zu keinem Zeitpunkt Vertragspartner,
            Erfüllungsgehilfe oder Vertreter einer der Parteien.
          </Typography>
        </li>
        <li>
          <Typography variant="body1">
            <strong>Keine Güte- oder Qualitätsgarantie:</strong> Die Benennung von Kliniken oder Ärzten
            beruht auf algorithmischen Zuordnungen anhand der vom Nutzer geschilderten Symptome und
            allgemein zugänglichen Fachgebietsangaben. Dies stellt weder eine Bewertung noch eine Garantie
            oder Zusicherung bezüglich der fachlichen Eignung, Qualität, Behandlungsstandards oder des
            Heilungserfolgs der gelisteten Einrichtungen dar.
          </Typography>
        </li>
        <li>
          <Typography variant="body1">
            <strong>Ausschluss der Haftung für Behandlungsfolgen:</strong> MedMatch übernimmt keinerlei
            Haftung für Diagnose- oder Behandlungsfehler, Aufklärungsversäumnisse, körperliche Schäden,
            wirtschaftliche Nachteile oder sonstige Folgen, die aus einer Konsultation oder Behandlung bei
            einem auf der Plattform aufgeführten Behandler resultieren.
          </Typography>
        </li>
      </ul>

      {/* ── 5. Pflichten der Nutzer ── */}
      <Typography variant="h5">5. Pflichten und Verhaltenskodex der Nutzer</Typography>
      <Typography variant="body1">
        Jeder registrierte Nutzer verpflichtet sich im Rahmen der Nutzung:
      </Typography>
      <ul>
        <li><Typography variant="body1">Wahrheitsgemäße Angaben bei der Registrierung und der Schilderung von Symptomen zu machen</Typography></li>
        <li><Typography variant="body1">Zugangsdaten und Passwörter sorgfältig geheim zu halten und vor dem unbefugten Zugriff Dritter zu schützen</Typography></li>
        <li><Typography variant="body1">Keine Ferndiagnosen, heilkundlichen Ratschläge oder unzulässigen Heilsversprechen gegenüber anderen Nutzern abzugeben</Typography></li>
        <li><Typography variant="body1">Keine vertraulichen Gesundheitsdaten oder Identitätsangaben anderer Nutzer ohne deren ausdrückliche Zustimmung zu verbreiten</Typography></li>
        <li><Typography variant="body1">Jegliche kommerzielle Werbung, den Vertrieb von Heilmitteln, Nahrungsergänzungsmitteln oder Dienstleistungen Dritter zu unterlassen</Typography></li>
      </ul>

      {/* ── 6. Haftungsbeschränkung nach deutschem Recht ── */}
      <Typography variant="h5">6. Haftungsbeschränkung nach den Vorschriften des BGB</Typography>
      <Typography variant="body1">
        Für die Haftung von MedMatch auf Schadens- oder Aufwendungsersatz gelten unbeschadet der gesetzlichen
        Bestimmungen folgende Regelungen:
      </Typography>
      <ul>
        <li>
          <Typography variant="body1">
            MedMatch haftet unbeschränkt für Schäden aus der Verletzung des Lebens, des Körpers oder der
            Gesundheit, die auf einer vorsätzlichen oder fahrlässigen Pflichtverletzung beruhen.
          </Typography>
        </li>
        <li>
          <Typography variant="body1">
            MedMatch haftet unbeschränkt für Schäden, die auf einer vorsätzlichen oder grob fahrlässigen
            Pflichtverletzung von MedMatch oder ihrer gesetzlichen Vertreter oder Erfüllungsgehilfen beruhen,
            sowie nach den zwingenden Bestimmungen des Produkthaftungsgesetzes.
          </Typography>
        </li>
        <li>
          <Typography variant="body1">
            Bei einfacher (leichter) Fahrlässigkeit haftet MedMatch nur für die Verletzung wesentlicher
            Vertragspflichten (sog. Kardinalpflichten, deren Erfüllung die ordnungsgemäße Durchführung des
            Vertrages überhaupt erst ermöglicht und auf deren Einhaltung der Nutzer regelmäßig vertrauen
            darf). In diesem Fall ist die Haftung der Höhe nach auf den bei Vertragsschluss vorhersehbaren,
            vertragstypischen Schaden begrenzt.
          </Typography>
        </li>
        <li>
          <Typography variant="body1">
            Soweit die Plattform dem Nutzer unentgeltlich bereitgestellt wird, ist die Haftung von MedMatch
            gemäß § 599 BGB analog auf Vorsatz und grobe Fahrlässigkeit beschränkt.
          </Typography>
        </li>
      </ul>

      {/* ── 7. Urheberrecht und Schutzrechte ── */}
      <Typography variant="h5">7. Gewerbliche Schutzrechte und Urheberrechte</Typography>
      <Typography variant="body1">
        Alle Inhalte, Designs, Logos, Datenbanken, Texte und Softwarekomponenten der Plattform MedMatch
        sind urheberrechtlich sowie markenrechtlich geschützt. Die Nutzung ist ausschließlich für den
        bestimmungsgemäßen persönlichen Gebrauch im Rahmen dieser Nutzungsbedingungen gestattet.
      </Typography>

      {/* ── 8. Kündigung und Sperrung ── */}
      <Typography variant="h5">8. Kündigung und Kontosperrung</Typography>
      <Typography variant="body1">
        Der Nutzer kann das Nutzungsverhältnis jederzeit ohne Einhaltung einer Frist durch Löschung seines
        Benutzerkontos in den Kontoeinstellungen beenden. MedMatch ist berechtigt, Nutzerkonten bei
        schwerwiegenden Verstößen gegen diese Nutzungsbedingungen oder gesetzliche Bestimmungen vorübergehend
        oder dauerhaft zu sperren.
      </Typography>

      {/* ── 9. Anwendbares Recht und Gerichtsstand ── */}
      <Typography variant="h5">9. Anwendbares Recht und Gerichtsstand</Typography>
      <Typography variant="body1">
        Für diese Nutzungsbedingungen und die gesamten Rechtsbeziehungen zwischen MedMatch und dem Nutzer
        gilt das Recht der <strong>Bundesrepublik Deutschland</strong> unter Ausschluss des UN-Kaufrechts (CISG).
      </Typography>
      <Typography variant="body1" sx={{ mt: 1 }}>
        Ist der Nutzer Verbraucher mit gewöhnlichem Aufenthalt in der EU, bleiben die zwingenden
        Verbraucherschutzbestimmungen des Staates, in dem er seinen gewöhnlichen Aufenthalt hat, unberührt.
      </Typography>

      {/* ── 10. Verbraucherstreitbeilegung ── */}
      <Typography variant="h5">10. Streitbeilegung für Verbraucher</Typography>
      <Typography variant="body1">
        Die Europäische Kommission stellt eine Plattform zur Online-Streitbeilegung (OS) bereit, die Sie
        unter <strong>https://ec.europa.eu/consumers/odr</strong> finden.
      </Typography>
      <Typography variant="body1" sx={{ mt: 1 }}>
        MedMatch ist weder verpflichtet noch bereit, an einem Streitbeilegungsverfahren vor einer
        Verbraucherschlichtungsstelle nach dem Verbraucherstreitbeilegungsgesetz (VSBG) teilzunehmen.
      </Typography>

      {/* ── 11. Kontakt ── */}
      <Typography variant="h5">11. Kontakt</Typography>
      <Typography variant="body1">
        Bei Fragen zu diesen Nutzungsbedingungen können Sie sich jederzeit an uns wenden:
      </Typography>
      <ul>
        <li><Typography variant="body1"><strong>E-Mail:</strong> legal@medmatch.app</Typography></li>
      </ul>
    </>
  );
}
