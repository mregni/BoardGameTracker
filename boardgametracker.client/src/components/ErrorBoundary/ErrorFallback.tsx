import { useTranslation } from 'react-i18next';
import { FallbackProps } from 'react-error-boundary';

import { BgtText } from '../BgtText/BgtText';
import { BgtCard } from '../BgtCard/BgtCard';
import { BgtButton } from '../BgtButton/BgtButton';

import AlertTriangle from '@/assets/icons/alert-triangle.svg?react';

export const ErrorFallback = ({ error, resetErrorBoundary }: FallbackProps) => {
  const { t } = useTranslation();

  return (
    <div className="min-h-screen bg-background flex items-center justify-center p-4">
      <BgtCard className="max-w-2xl w-full">
        <div className="flex flex-col items-center text-center gap-6">
          <div className="bg-error/20 rounded-full p-6">
            <AlertTriangle className="size-16 text-error" />
          </div>

          <div className="space-y-2">
            <BgtText color="white" size="5">
              {t('error.something-went-wrong')}
            </BgtText>
            <BgtText color="white" opacity={70}>
              {t('error.unexpected-error')}
            </BgtText>
          </div>

          {process.env.NODE_ENV === 'development' && (
            <details className="w-full text-left">
              <summary className="cursor-pointer text-white/60 hover:text-white/80 mb-2">
                Error Details (Development Only)
              </summary>
              <pre className="bg-error/10 border border-error/30 rounded-lg p-4 overflow-auto text-sm text-error">
                {error.message}
                {error.stack && (
                  <>
                    {'\n\n'}
                    {error.stack}
                  </>
                )}
              </pre>
            </details>
          )}

          <div className="flex gap-3">
            <BgtButton onClick={resetErrorBoundary} variant="primary">
              {t('common.try-again')}
            </BgtButton>
            <BgtButton onClick={() => (window.location.href = '/')} variant="primary">
              {t('common.go-home')}
            </BgtButton>
          </div>
        </div>
      </BgtCard>
    </div>
  );
};
