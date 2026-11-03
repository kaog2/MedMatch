import { Box, Typography } from '@mui/material';

export default function LegalNoticeDe() {
  return (
    <>
      <Typography variant="body1" sx={{ mb: 3 }}>
        Pflichtangaben und rechtliche Hinweise für das digitale Informations- und Vermittlungsangebot
        von MedMatch gemäß § 5 des Digitale-Dienste-Gesetzes (DDG) und § 18 des Medienstaatsvertrags (MStV).
      </Typography>

      {/* ── 1. Angaben gemäß § 5 DDG ── */}
      <Typography variant="h5">1. Angaben gemäß § 5 Digitale-Dienste-Gesetz (DDG)</Typography>
      <Typography variant="body1">
        Dienstanbieter und Betreiber der Plattform:
      </Typography>
      <ul>
        <li><Typography variant="body1"><strong>Plattform:</strong> MedMatch Health Platform</Typography></li>
        <li><Typography variant="body1"><strong>Unternehmensgegenstand:</strong> Bereitstellung einer digitalen Kommunikations- und Vernetzungsplattform für Patienten sowie Orientierungshilfe zu medizinischen Fachzentren</Typography></li>
        <li><Typography variant="body1"><strong>Website:</strong> https://medmatch.app</Typography></li>
        <li><Typography variant="body1"><strong>E-Mail:</strong> legal@medmatch.app</Typography></li>
        <li><Typography variant="body1"><strong>Technischer Support:</strong> support@medmatch.app</Typography></li>
      </ul>

      {/* ── 2. Verantwortlich für den Inhalt ── */}
      <Typography variant="h5">2. Verantwortlich für journalistisch-redaktionelle Inhalte</Typography>
      <Typography variant="body1">
        Verantwortlich im Sinne des § 18 Abs. 2 MStV:
      </Typography>
      <ul>
        <li><Typography variant="body1">MedMatch Redaktion & Plattformleitung</Typography></li>
        <li><Typography variant="body1">Kontakt: redaktion@medmatch.app / legal@medmatch.app</Typography></li>
      </ul>

      {/* ── 3. Natur des Dienstes und medizinischer Hinweis ── */}
      <Typography variant="h5">3. Natur des Dienstes und Ausschluss heilkundlicher Tätigkeit</Typography>
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
          Reine Kommunikationsinfrastruktur
        </Typography>
        <Typography variant="body1">
          MedMatch ist kein medizinischer Leistungserbringer und übt keine Heilkunde im Sinne des
          Heilpraktikergesetzes oder der Bundesärzteordnung aus. Die Plattform erbringt weder
          telemedizinische Konsultationen noch Ferndiagnosen noch individuelle Therapieempfehlungen.
          Etwaige Behandlungsverhältnisse entstehen ausschließlich unmittelbar zwischen dem Nutzer und
          den jeweiligen Fachärzten bzw. Kliniken.
        </Typography>
      </Box>

      {/* ── 4. Haftung für Inhalte ── */}
      <Typography variant="h5">4. Haftung für eigene und fremde Inhalte (§ 7 DDG)</Typography>
      <Typography variant="body1">
        Als Diensteanbieter sind wir gemäß § 7 Abs. 1 DDG für eigene Inhalte auf diesen Seiten nach den
        allgemeinen Gesetzen verantwortlich.
      </Typography>
      <Typography variant="body1" sx={{ mt: 1.5 }}>
        Nach §§ 8 bis 10 DDG sind wir als Diensteanbieter jedoch nicht verpflichtet, übermittelte oder
        gespeicherte fremde Informationen (z. B. Beiträge, Symptombeschreibungen oder Erfahrungsberichte
        von Nutzern) proaktiv zu überwachen oder nach Umständen zu forschen, die auf eine rechtswidrige
        Tätigkeit hinweisen.
      </Typography>
      <Typography variant="body1" sx={{ mt: 1.5 }}>
        Verpflichtungen zur Entfernung oder Sperrung der Nutzung von Informationen nach den allgemeinen
        Gesetzen bleiben hiervon unberührt. Eine diesbezügliche Haftung ist jedoch erst ab dem Zeitpunkt
        der Kenntnis einer konkreten Rechtsverletzung möglich. Bei Bekanntwerden von entsprechenden
        Rechtsverletzungen werden wir diese Inhalte unverzüglich entfernen.
      </Typography>

      {/* ── 5. Haftung für externe Links ── */}
      <Typography variant="h5">5. Haftung für Hyperlinks zu Webseiten Dritter</Typography>
      <Typography variant="body1">
        Unser Angebot enthält Verlinkungen zu externen Webseiten Dritter (z. B. Webseiten empfohlener
        Kliniken, Behandlungszentren oder Fachärzte), auf deren Inhalte wir keinen Einfluss haben.
        Deshalb können wir für diese fremden Inhalte auch keine Gewähr übernehmen. Für die Inhalte der
        verlinkten Seiten ist stets der jeweilige Anbieter oder Betreiber der Seiten verantwortlich.
      </Typography>
      <Typography variant="body1" sx={{ mt: 1.5 }}>
        Die verlinkten Seiten wurden zum Zeitpunkt der Verlinkung auf mögliche Rechtsverstöße überprüft.
        Rechtswidrige Inhalte waren zum Zeitpunkt der Verlinkung nicht erkennbar. Eine permanente
        inhaltliche Kontrolle der verlinkten Seiten ist jedoch ohne konkrete Anhaltspunkte einer
        Rechtsverletzung nicht zumutbar. Bei Bekanntwerden von Rechtsverletzungen werden wir derartige
        Links unverzüglich entfernen.
      </Typography>

      {/* ── 6. Urheberrecht ── */}
      <Typography variant="h5">6. Urheberrecht und gewerblicher Rechtsschutz</Typography>
      <Typography variant="body1">
        Die durch die Seitenbetreiber erstellten Inhalte und Werke auf diesen Seiten unterliegen dem
        deutschen Urheberrecht. Die Vervielfältigung, Bearbeitung, Verbreitung und jede Art der Verwertung
        außerhalb der Grenzen des Urheberrechtes bedürfen der vorherigen schriftlichen Zustimmung des
        jeweiligen Autors bzw. Erstellers.
      </Typography>
      <Typography variant="body1" sx={{ mt: 1.5 }}>
        Downloads und Kopien dieser Seite sind nur für den privaten, nicht kommerziellen Gebrauch
        gestattet. Soweit die Inhalte auf dieser Seite nicht vom Betreiber erstellt wurden, werden die
        Urheberrechte Dritter beachtet und als solche gekennzeichnet.
      </Typography>

      {/* ── 7. Verbraucherstreitbeilegung ── */}
      <Typography variant="h5">7. Streitbeilegung vor Verbraucherschlichtungsstellen</Typography>
      <Typography variant="body1">
        Die Europäische Kommission stellt eine Plattform zur Online-Streitbeilegung (OS) bereit:{' '}
        <a href="https://ec.europa.eu/consumers/odr/" target="_blank" rel="noopener noreferrer" style={{ color: '#00695c' }}>
          https://ec.europa.eu/consumers/odr/
        </a>.
      </Typography>
      <Typography variant="body1" sx={{ mt: 1 }}>
        Wir sind nicht bereit oder verpflichtet, an Streitbeilegungsverfahren vor einer
        Verbraucherschlichtungsstelle teilzunehmen.
      </Typography>
    </>
  );
}
