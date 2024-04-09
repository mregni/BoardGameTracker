import {useTranslation} from 'react-i18next';

export const BgtNoData = () => {
  const {t} = useTranslation();
  
  return (
    <div className='flex justify-center'>
      {t('common.no-data')}
    </div>
  )
}