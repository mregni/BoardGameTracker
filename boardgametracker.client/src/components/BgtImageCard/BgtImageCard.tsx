import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useState } from 'react';
import { cx } from 'class-variance-authority';

import { BgtDeleteModal } from '../Modals/BgtDeleteModal';
import { BgtText } from '../BgtText/BgtText';
import { BgtHiddenEditDropdown } from '../BgtDropdown/BgtEditDropdown';
import { getColorFromGameState, getItemStateTranslationKey } from '../../utils/ItemStateUtils';

import { StringToRgb } from '@/utils/stringUtils';
import { useToast } from '@/providers/BgtToastProvider';
import { useGames } from '@/pages/Games/hooks/useGames';
import { GameState } from '@/models';

interface Props {
  title: string;
  state?: GameState;
  image: string | null;
  link: string;
  id: number;
}

export const BgtImageCard = (props: Props) => {
  const { title, image, state = null, link, id } = props;
  const { t } = useTranslation();
  const [openDeleteModal, setOpenDeleteModal] = useState(false);
  const { showInfoToast } = useToast();

  const onDeleteSuccess = () => {
    setOpenDeleteModal(false);
    showInfoToast(t('game.notifications.deleted'));
  };
  const { deleteGame } = useGames({ onDeleteSuccess });

  return (
    <div className="flex flex-col justify-center cursor-pointer flex-nowrap relative gap-1 group">
      <Link to={link}>
        <div
          style={{ '--image-url': `url(${image})`, '--fallback-color': StringToRgb(title) }}
          className={cx(
            'w-full relative overflow-hidden aspect-square z-10 rounded-xl flex justify-end flex-col gap-3 pb-4 px-3',
            `bg-cover bg-no-repeat bg-center`,
            image && 'bg-[image:var(--image-url)]',
            !image && `bg-[var(--fallback-color)]`
          )}
        ></div>
      </Link>
      <div className="flex flex-row justify-between items-end">
        <div className="flex flex-col items-start justify-start">
          <Link to={link}>
            {state !== null && (
              <BgtText
                size="1"
                className="line-clamp-1 uppercase w-full"
                weight="medium"
                color={getColorFromGameState(state)}
              >
                {t(getItemStateTranslationKey(state))}
              </BgtText>
            )}
            <BgtText size="4" className="line-clamp-1 uppercase w-full" weight="medium">
              {title}
            </BgtText>
          </Link>
        </div>
        <BgtHiddenEditDropdown onDelete={() => setOpenDeleteModal(true)} onEdit={() => alert('editing')} />
      </div>
      <BgtDeleteModal
        title={title}
        open={openDeleteModal}
        close={() => setOpenDeleteModal(false)}
        onDelete={() => deleteGame(id)}
        description={t('common.delete.description', { title: title })}
      />
    </div>
  );
};
