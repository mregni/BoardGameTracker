import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

import { BgtText } from '../../../components/BgtText/BgtText';
import { BgtHeading } from '../../../components/BgtHeading/BgtHeading';
import { BgtCard } from '../../../components/BgtCard/BgtCard';
import BgtButton from '../../../components/BgtButton/BgtButton';

interface Props {
  gameId: number;
}

export const BgtNoSessions = (props: Props) => {
  const { gameId } = props;
  const { t } = useTranslation();
  const navigate = useNavigate();

  return (
    <BgtCard className="p-6">
      <div className="flex flex-row justify-between items-center">
        <div className=" flex flex-col gap-3">
          <BgtHeading size="6">{t('common.no-sessions.title')}</BgtHeading>
          <BgtText> {t('common.no-sessions.content')}</BgtText>
        </div>
        <div>
          <BgtButton size="3" onClick={() => navigate(`/sessions/create/${gameId}`)}>
            {t('game.add')}
          </BgtButton>
        </div>
      </div>
    </BgtCard>
  );
};
