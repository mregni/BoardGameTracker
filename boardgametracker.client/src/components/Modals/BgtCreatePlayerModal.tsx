import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';

import { zodResolver } from '@hookform/resolvers/zod';
import * as Form from '@radix-ui/react-form';
import { Button, Dialog } from '@radix-ui/themes';

import { CreatePlayPlayer, CreatePlayPlayerNoScoring, CreatePlayPlayerNoScoringSchema, CreatePlayPlayerSchema } from '../../models/Plays/CreatePlay';

import { useLocations } from '../../hooks/useLocations';
import { usePlayers } from '../../hooks/usePlayers';
import { BgtInputField } from '../BgtForm/BgtInputField';
import { BgtSelect } from '../BgtForm/BgtSelect';
import { BgtSwitch } from '../BgtSwitch/BgtSwitch';

interface Props {
  open: boolean;
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
  hasScoring: boolean;
  onClose: (player: CreatePlayPlayer | CreatePlayPlayerNoScoring) => void;
}

export const BgtCreatePlayerModal = (props: Props) => {
  const { open, setOpen, hasScoring, onClose } = props;
  const { t } = useTranslation();
  const { players } = usePlayers();
  const { locations } = useLocations();

  type PlayType<T extends boolean> = T extends true ? CreatePlayPlayer : CreatePlayPlayerNoScoring;
  type CreatePlayType = PlayType<typeof hasScoring>;

  const { register, handleSubmit, control } = useForm<CreatePlayType>({
    resolver: zodResolver(hasScoring ? CreatePlayPlayerSchema : CreatePlayPlayerNoScoringSchema),
    defaultValues: {
      firstPlay: false,
      won: false,
      isBot: false,
    },
  });

  if (players === undefined || locations === undefined) return null;

  const onSubmit = (data: CreatePlayPlayer | CreatePlayPlayerNoScoring) => {
    onClose && onClose(data);
  };

  return (
    <Dialog.Root open={open}>
      <Dialog.Content>
        <Dialog.Title>{t('playplayer.new.title')}</Dialog.Title>
        <Dialog.Description>{t('playplayer.new.description')}</Dialog.Description>
        <form onSubmit={(event) => void handleSubmit(onSubmit)(event)}>
          <div className="flex flex-col gap-4 mt-3 mb-6">
            <BgtSelect
              control={control}
              label={t('playplayer.new.player.label')}
              name="playerId"
              items={players.map((value) => ({
                label: value.name,
                value: value.id.toString(),
              }))}
            />
            {hasScoring && (
              <BgtInputField name="score" type="number" valueAsNumber register={register} control={control} label={t('playplayer.new.score.label')} />
            )}
            <BgtSwitch label={t('playplayer.new.won.label')} control={control} name="won" className="mt-2" />
            <BgtSwitch label={t('playplayer.new.first-play.label')} control={control} name="firstPlay" className="mt-2" />
            <BgtSwitch label={t('playplayer.new.bot.label')} control={control} name="isBot" className="mt-2" />
          </div>
          <div className="flex justify-end gap-3">
            <Dialog.Close>
              <>
                <Form.Submit asChild>
                  <Button type="submit" variant="surface" color="orange">
                    {t('playplayer.new.save')}
                  </Button>
                </Form.Submit>
                <Button variant="surface" color="gray" onClick={() => setOpen(false)}>
                  {t('common.cancel')}
                </Button>
              </>
            </Dialog.Close>
          </div>
        </form>
      </Dialog.Content>
    </Dialog.Root>
  );
};
