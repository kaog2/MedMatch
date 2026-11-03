import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import LegalPage, { LegalJurisdiction } from './LegalPage';
import TermsOfUseCl from './legal/TermsOfUseCl';
import TermsOfUseDe from './legal/TermsOfUseDe';

interface TermsOfUseProps {
  defaultJurisdiction?: LegalJurisdiction;
}

export default function TermsOfUse({ defaultJurisdiction }: TermsOfUseProps) {
  const { i18n } = useTranslation();
  const initialJurisdiction: LegalJurisdiction =
    defaultJurisdiction ?? (i18n.language && i18n.language.startsWith('de') ? 'de' : 'cl');

  const [jurisdiction, setJurisdiction] = useState<LegalJurisdiction>(initialJurisdiction);

  const isGerman = jurisdiction === 'de';

  return (
    <LegalPage
      title={isGerman ? 'Allgemeine Nutzungsbedingungen (AGB)' : 'Términos y Condiciones de Uso'}
      lastUpdated={isGerman ? '3. November 2026' : '5 de octubre de 2026'}
      jurisdiction={jurisdiction}
      onJurisdictionChange={setJurisdiction}
    >
      {isGerman ? <TermsOfUseDe /> : <TermsOfUseCl />}
    </LegalPage>
  );
}
