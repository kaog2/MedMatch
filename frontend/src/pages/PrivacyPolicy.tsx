import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import LegalPage, { LegalJurisdiction } from './LegalPage';
import PrivacyPolicyCl from './legal/PrivacyPolicyCl';
import PrivacyPolicyDe from './legal/PrivacyPolicyDe';

interface PrivacyPolicyProps {
  defaultJurisdiction?: LegalJurisdiction;
}

export default function PrivacyPolicy({ defaultJurisdiction }: PrivacyPolicyProps) {
  const { i18n } = useTranslation();
  const initialJurisdiction: LegalJurisdiction =
    defaultJurisdiction ?? (i18n.language && i18n.language.startsWith('de') ? 'de' : 'cl');

  const [jurisdiction, setJurisdiction] = useState<LegalJurisdiction>(initialJurisdiction);

  const isGerman = jurisdiction === 'de';

  return (
    <LegalPage
      title={isGerman ? 'Datenschutzerklärung' : 'Política de Privacidad'}
      lastUpdated={isGerman ? '3. November 2026' : '5 de octubre de 2026'}
      jurisdiction={jurisdiction}
      onJurisdictionChange={setJurisdiction}
    >
      {isGerman ? <PrivacyPolicyDe /> : <PrivacyPolicyCl />}
    </LegalPage>
  );
}
