import { t } from 'i18next';
import { useState } from 'react';
import { ArrayPath, Control, useController, useFieldArray } from 'react-hook-form';

import { ClockIcon, PencilIcon, RocketLaunchIcon, TrashIcon, TrophyIcon } from '@heroicons/react/24/outline';
import { Badge, Button, Text } from '@radix-ui/themes';

import { usePlayers } from '../../hooks/usePlayers';
import { CreatePlay, CreatePlayPlayer, CreatePlayPlayerNoScoring } from '../../models/Plays/CreatePlay';
import { StringToHsl } from '../../utils/stringUtils';
import { BgtAvatar } from '../BgtAvatar/BgtAvatar';
import { BgtIconButton } from '../BgtIconButton/BgtIconButton';
import { BgtCreatePlayerModal } from '../Modals/BgtCreatePlayerModal';
import { BgtFormErrors } from './BgtFormErrors';

interface Props {
  name: ArrayPath<CreatePlay>;
  control: Control<CreatePlay>;
  hasScoring: boolean;
}

export const BgtPlayerSelector = (props: Props) => {
  const { name, control, hasScoring } = props;
  const { byId } = usePlayers();
  const [openCreateNewPlayerModal, setOpenCreateNewPlayerModal] = useState(false);

  const {
    fieldState: { error },
  } = useController({ name, control });

  const {
    fields: players,
    append,
    remove,
  } = useFieldArray<CreatePlay>({
    name: name,
    control,
  });

  const closeNewPlayPlayer = (player: CreatePlayPlayer | CreatePlayPlayerNoScoring) => {
    append(player);
    setOpenCreateNewPlayerModal(false);
  };

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-col gap-1">
        <Button className="w-fit" type="button" onClick={() => setOpenCreateNewPlayerModal(true)}>
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
      {openCreateNewPlayerModal && (
        <BgtCreatePlayerModal
          open={openCreateNewPlayerModal}
          setOpen={setOpenCreateNewPlayerModal}
          hasScoring={hasScoring}
          onClose={closeNewPlayPlayer}
        />
      )}
    </div>
  );
};
