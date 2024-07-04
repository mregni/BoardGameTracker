import { useTranslation } from 'react-i18next';

import { BgtText } from '../BgtText/BgtText';
import { BgtAvatar } from '../BgtAvatar/BgtAvatar';
import { StringToHsl } from '../../utils/stringUtils';
import { MostWinner } from '../../models';

interface Props {
  player: MostWinner | null;
}

export const BgtMostWinnerCard = (props: Props) => {
  const { player } = props;
  const { t } = useTranslation();

  if (player === undefined) return null;

  return (
    <div className="flex flex-row justify-between items-center bg-gradient-to-r from-[#9A02FB1A] to-[#09FFC41A] rounded-lg p-3">
      <div className="flex flex-row gap-2">
        <BgtAvatar image={player?.image} title={player?.name} color={StringToHsl(player?.name)} size="large" />
        <div className="flex flex-col justify-center gap-0">
          <BgtText weight="medium" size="3" className="uppercase">
            {player?.name}
          </BgtText>
          <BgtText size="1" className="uppercase text-mint-green">
            {t('statistics.most-wint')}
          </BgtText>
        </div>
      </div>
      <div className="flex flex-col gap2 items-end">
        <BgtText size="3" className="uppercase">
          {t('common.total-wins')}
        </BgtText>
        <BgtText size="1" className="uppercase text-lime-green">
          {player?.totalWins}
        </BgtText>
      </div>
    </div>
  );
};
