import {useEffect, useState} from 'react';
import {useTranslation} from 'react-i18next';
import {useParams} from 'react-router-dom';

import {Button} from '@radix-ui/themes';

import {BgtCard} from '../../../components/BgtCard/BgtCard';
import {BgtMobilePlayCard} from '../../../components/BgtCard/BgtMobilePlayCard';
import {BgtNoData} from '../../../components/BgtNoData/BgtNoData';
import {usePlayerPlays} from '../../../hooks/usePlays';
import {Play} from '../../../models';

const pageCount = 10;

export const PlayerPlaysMobile = () => {
  const { id } = useParams();
  const { t } = useTranslation();
  const [page, setPage] = useState(0);
  const { plays, totalCount } = usePlayerPlays(id, page, pageCount);
  const [fullPlayList, setFullPlayList] = useState<Play[]>([]);

  useEffect(() => {
    setFullPlayList((old) => [...new Set([old, plays].flat())])
  }, [plays]);

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
      {fullPlayList.map((play, i) => <BgtMobilePlayCard key={play.id} index={i} play={play} showGame />)}
      <Button
        size="2"
        variant="outline"
        className='hover:cursor-pointer'
        onClick={() => setPage((old) => old + 1)}
        disabled={fullPlayList.length >= totalCount}
      >
        {t('common.load-more')}
      </Button>
    </div>
  )
}
