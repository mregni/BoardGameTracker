import { ArrayPath, Control, FieldArrayWithId, UseFieldArrayRemove, useController } from 'react-hook-form';
import { t } from 'i18next';
import { Badge, Button, Text } from '@radix-ui/themes';
import { ClockIcon, PencilIcon, RocketLaunchIcon, TrashIcon, TrophyIcon } from '@heroicons/react/24/outline';

import { BgtIconButton } from '../BgtIconButton/BgtIconButton';
import { BgtAvatar } from '../BgtAvatar/BgtAvatar';
import { StringToHsl } from '../../utils/stringUtils';
import { CreatePlay } from '../../models/Plays/CreatePlay';
import { usePlayers } from '../../hooks/usePlayers';

import { BgtFormErrors } from './BgtFormErrors';

interface Props {
  name: ArrayPath<CreatePlay>;
  control: Control<CreatePlay>;
  setModalOpen: React.Dispatch<React.SetStateAction<boolean>>;
  remove: UseFieldArrayRemove;
  players: FieldArrayWithId<CreatePlay>[];
}

export const BgtPlayerSelector = (props: Props) => {
  const { name, control, setModalOpen, remove, players } = props;
  const { byId } = usePlayers();

  const {
    fieldState: { error },
  } = useController({ name, control });

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-col gap-1">
        <Button className="w-fit" type="button" onClick={() => setModalOpen(true)}>
          {t('playplayer.new.players.add')}
        </Button>
        <BgtFormErrors error={error} />
      </div>
      {players.map((x, index) => (
        <div key={x.playerId} className="flex flex-row gap-3 md:max-w-96 justify-between">
          <div className="flex items-center gap-3">
            <BgtAvatar
              noTooltip
              size="large"
              title={byId(x.playerId)?.name}
              image={byId(x.playerId)?.image}
              color={StringToHsl(byId(x.playerId)?.name)}
            />
            <Text>{byId(x.playerId)?.name}</Text>
            {x.won && <TrophyIcon className="w-4  text-yellow-600" />}
            {x.firstPlay && <ClockIcon className="w-4 text-green-600" />}
            {x.isBot && <RocketLaunchIcon className="w-4 text-red-600" />}
            {'score' in x && x.score !== undefined && <Badge>{x.score}</Badge>}
          </div>
          <div className="flex items-center gap-1">
            <BgtIconButton size={17} icon={<PencilIcon />} onClick={() => console.log('boe')} />
            <BgtIconButton size={17} icon={<TrashIcon />} onClick={() => remove(index)} type="danger" />
          </div>
        </div>
      ))}
    </div>
  );
};
