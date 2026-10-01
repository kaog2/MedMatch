import { useQuery } from '@tanstack/react-query';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { translateTexts } from '../services/api';

/**
 * Translates user-created tag labels for display only. The source text remains
 * unchanged, so profile matching and diary analytics continue to use one
 * canonical saved value. The backend caches every translated result.
 */
export function useTranslatedTags(values: Iterable<string | null | undefined>) {
  const { i18n } = useTranslation();
  const targetLanguage = (i18n.resolvedLanguage ?? i18n.language ?? 'en').split('-')[0];
  const tags = useMemo(
    () => [...new Set(Array.from(values, (value) => value?.trim() ?? '').filter(Boolean))].sort(),
    [values],
  );
  const key = tags.join('\u0001');

  const query = useQuery({
    queryKey: ['tag-translations', targetLanguage, key],
    queryFn: () => translateTexts(tags, targetLanguage),
    enabled: targetLanguage !== 'en' && tags.length > 0,
    staleTime: Infinity,
    retry: 1,
  });

  return {
    translateTag: (value: string) => query.data?.[value] ?? value,
    isTranslating: query.isFetching,
  };
}
