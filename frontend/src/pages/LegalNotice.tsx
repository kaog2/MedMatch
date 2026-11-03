import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import LegalPage, { LegalJurisdiction } from './LegalPage';
import LegalNoticeCl from './legal/LegalNoticeCl';
import LegalNoticeDe from './legal/LegalNoticeDe';

interface LegalNoticeProps {
  defaultJurisdiction?: LegalJurisdiction;
}

export default function LegalNotice({ defaultJurisdiction }: LegalNoticeProps) {
  const { i18n } = useTranslation();
  const initialJurisdiction: LegalJurisdiction =
    defaultJurisdiction ?? (i18n.language && i18n.language.startsWith('de') ? 'de' : 'cl');

  const [jurisdiction, setJurisdiction] = useState<LegalJurisdiction>(initialJurisdiction);

  const isGerman = jurisdiction === 'de';

  return (
    <LegalPage
      title={isGerman ? 'Impressum (§ 5 DDG)' : 'Aviso Legal'}
      lastUpdated={isGerman ? '3. November 2026' : '5 de octubre de 2026'}
      jurisdiction={jurisdiction}
      onJurisdictionChange={setJurisdiction}
    >
      {isGerman ? <LegalNoticeDe /> : <LegalNoticeCl />}
    </LegalPage>
  );
}
