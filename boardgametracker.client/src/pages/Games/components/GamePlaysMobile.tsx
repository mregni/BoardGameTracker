import {useEffect, useState} from 'react';
import {useTranslation} from 'react-i18next';
import {useParams} from 'react-router-dom';

import {Button} from '@radix-ui/themes';

import {BgtCard} from '../../../components/BgtCard/BgtCard';
import {BgtMobilePlayCard} from '../../../components/BgtCard/BgtMobilePlayCard';
import {BgtNoData} from '../../../components/BgtNoData/BgtNoData';
import {useGamePlays} from '../../../hooks/usePlays';
import {useSettings} from '../../../hooks/useSettings';
import {Play} from '../../../models';

export const MobileDetails = () => {
  const { id } = useParams();
  const [page, setPage] = useState(0);
  const { plays, totalCount, isFetching } = useGamePlays(id, page, 10);
  const { t } = useTranslation();
  const { settings } = useSettings();

  const [fullPlayList, setFullPlayList] = useState<Play[]>([]);

  useEffect(() => {
    if(!isFetching) {
      setFullPlayList((old) => [...new Set([old, plays].flat())])
    }
  }, [plays, isFetching]);
  
  if (settings === undefined) return null;

  if (plays.length === 0) {
    return (
      <BgtCard
        title={t('games.cards.games')}
        contentStyle='bg-sky-800'>
        <BgtNoData />
      </BgtCard>
    )
  }

  return (
    <div className='rounded-md flex flex-col gap-3'>
      {fullPlayList.map((play, i) => <BgtMobilePlayCard key={play.id} index={i} play={play} />)}
      <Button
        size="2"
        variant="outline"
        className='hover:cursor-pointer'
        onClick={() => setPage((old) => old + 1)}
        disabled={fullPlayList.length >= totalCount || isFetching}
      >
        {t('common.load-more')}
      </Button>
    </div>
  )
}