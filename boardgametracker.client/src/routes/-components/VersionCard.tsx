import { useTranslation } from 'react-i18next';

import { VersionInfo } from '@/models/Settings/VersionInfo';
import { BgtText } from '@/components/BgtText/BgtText';
import { BgtFancyTextStatistic } from '@/components/BgtStatistic/BgtFancyTextStatistic';

interface Props {
  versionInfo: VersionInfo | undefined;
}

export const VersionCard = ({ versionInfo }: Props) => {
  const { t } = useTranslation();

  if (versionInfo === undefined) return null;

  return (
    <>
      {versionInfo && versionInfo.updateAvailable ? (
        <BgtFancyTextStatistic
          title={t('version.new')}
          content={`v${versionInfo.currentVersion} => v${versionInfo.latestVersion}`}
        />
      ) : (
        <div className="flex items-center justify-between text-xs text-white/40 p-3 border-t border-white/10">
          <BgtText color="white" size="3" opacity={60}>
            {t('version.version')}
          </BgtText>
          <BgtText color="white" opacity={60} className="font-mono">
            v{versionInfo?.currentVersion}
          </BgtText>
        </div>
      )}
    </>
  );
};
