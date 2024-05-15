import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useEffect, useState } from 'react';
import { Button } from '@radix-ui/themes';

import { Play } from '../../../models';
import { useSettings } from '../../../hooks/useSettings';
import { useGamePlays } from '../../../hooks/usePlays';
import { BgtDeleteModal } from '../../../components/Modals/BgtDeleteModal';
import { BgtNoData } from '../../../components/BgtNoData/BgtNoData';
import { BgtMobilePlayCard } from '../../../components/BgtCard/BgtMobilePlayCard';
import { BgtCard } from '../../../components/BgtCard/BgtCard';

export const MobileDetails = () => {
  const { id } = useParams();
  const [page, setPage] = useState(0);
  const { plays, totalCount, isFetching, deletePlay } = useGamePlays(id, page, 10);
  const { t } = useTranslation();
  const { settings } = useSettings();
  const [openDeleteModal, setOpenDeleteModal] = useState(false);
  const [playIdToDelete, setPlayIdToDelete] = useState<number | null>(null);

  const deletePlayInternal = () => {
    if (playIdToDelete !== null) {
      void deletePlay(playIdToDelete).finally(() => {
        setFullPlayList([]);
        setOpenDeleteModal(false);
        setPlayIdToDelete(null);
      });
    }
  };

  const openDeleteModalInternal = (id: number) => {
    setOpenDeleteModal(true);
    setPlayIdToDelete(id);
  };

  const [fullPlayList, setFullPlayList] = useState<Play[]>([]);

  useEffect(() => {
    if (!isFetching) {
      setFullPlayList((old) => [...new Set([old, plays].flat())]);
    }
  }, [plays, isFetching]);

  if (settings === undefined) return null;

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
        <BgtMobilePlayCard key={play.id} index={i} play={play} onDelete={openDeleteModalInternal} />
      ))}
      <Button
        size="2"
        variant="outline"
        className="hover:cursor-pointer"
        onClick={() => setPage((old) => old + 1)}
        disabled={fullPlayList.length >= totalCount || isFetching}
      >
        {t('common.load-more')}
      </Button>
      <BgtDeleteModal title={t('common.game-play')} open={openDeleteModal} setOpen={setOpenDeleteModal} onDelete={deletePlayInternal} />
    </div>
  );
};
