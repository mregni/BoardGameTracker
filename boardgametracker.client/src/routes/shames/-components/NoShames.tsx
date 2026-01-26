import { useTranslation } from 'react-i18next';

import { BgtCard } from '@/components/BgtCard/BgtCard';
import Thumb from '@/assets/icons/thumb.svg?react';

export const NoShames = () => {
  const { t } = useTranslation();

  return (
    <BgtCard>
      <div className="p-8 text-center">
        <Thumb className="text-green-500 mx-auto mb-4" />
        <h3 className="text-xl text-white mb-2">{t('shames.none.title')}</h3>
        <p className="text-gray-400">{t('shames.none.description')}</p>
      </div>
    </BgtCard>
  );
};
