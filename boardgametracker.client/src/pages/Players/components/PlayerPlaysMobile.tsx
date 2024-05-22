import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useEffect, useState } from 'react';
import { Button } from '@radix-ui/themes';

import { Play } from '../../../models';
import { usePlayerPlays } from '../../../hooks/usePlays';
import { BgtNoData } from '../../../components/BgtNoData/BgtNoData';
import { BgtMobilePlayCard } from '../../../components/BgtCard/BgtMobilePlayCard';
import { BgtCard } from '../../../components/BgtCard/BgtCard';

const pageCount = 10;

export const PlayerPlaysMobile = () => {
  const { id } = useParams();
  const { t } = useTranslation();
  const [page, setPage] = useState(0);
  const { plays, totalCount, isFetching } = usePlayerPlays(id, page, pageCount);
  const [fullPlayList, setFullPlayList] = useState<Play[]>([]);

  useEffect(() => {
    if (!isFetching) {
      setFullPlayList((old) => [...new Set([old, plays].flat())]);
    }
  }, [isFetching, plays]);

  if (plays.length === 0) {
    return (
      <BgtCard title={t('games.cards.games')} contentStyle="bg-sky-800">
        <BgtNoData />
      </BgtCard>
    );
  }

  return (
    <div className="flex flex-col gap-3">
      {fullPlayList.map((play, i) => (
        <BgtMobilePlayCard key={play.id} index={i} play={play} showGame />
      ))}
      <Button
        size="2"
        variant="outline"
        className="hover:cursor-pointer"
        onClick={() => setPage((old) => old + 1)}
        disabled={fullPlayList.length >= totalCount}
      >
        {t('common.load-more')}
      </Button>
    </div>
  );
};
