import { t } from 'i18next';
import { ReactNode, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useParams } from 'react-router-dom';

import { zodResolver } from '@hookform/resolvers/zod';
import { Button, Text } from '@radix-ui/themes';

import { BgtComboBox } from '../../components/BgtForm/BgtComboBox';
import { BgtInputField } from '../../components/BgtForm/BgtInputField';
import { BgtPlayerSelector } from '../../components/BgtForm/BgtPlayerSelector';
import { BgtSelectNoLabel } from '../../components/BgtForm/BgtSelectNoLabel';
import { BgtTextArea } from '../../components/BgtForm/BgtTextArea';
import { BgtPage } from '../../components/BgtLayout/BgtPage';
import { BgtPageContent } from '../../components/BgtLayout/BgtPageContent';
import { BgtNewLocationModal } from '../../components/Modals/BgtNewLocationModal';
import { useGame } from '../../hooks/useGame';
import { useGames } from '../../hooks/useGames';
import { useLocations } from '../../hooks/useLocations';
import { CreatePlay, CreatePlaySchema } from '../../models/Plays/CreatePlay';

interface EmptyFormRowProps {
  right: ReactNode;
}

interface FormRowProps extends EmptyFormRowProps {
  title: string;
  subTitle?: string;
}

const FormRow = (props: FormRowProps) => {
  const { title, subTitle, right } = props;
  return (
    <div className="grid grid-cols-1 gap-1 md:gap-0 md:grid-cols-2 justify-center md:divide-x md:divide-blue-500 w-full min-h-16">
      <div className="pr-3 flex flex-col md:items-end">
        <Text size="3" weight="bold">
          {title}
        </Text>
        {subTitle && (
          <Text size="1" className="text-left md:text-right">
            {subTitle}
          </Text>
        )}
      </div>
      <div className="md:pl-3">{right}</div>
    </div>
  );
};

const EmptyFormRow = (props: EmptyFormRowProps) => {
  const { right } = props;
  return (
    <div className="grid grid-cols-1 gap-1 md:gap-0 md:grid-cols-2 justify-center w-full min-h-16">
      <div className="md:col-start-2 md:pl-3">{right}</div>
    </div>
  );
};

export const CreatePlayPage = () => {
  const { gameId } = useParams();
  const { game } = useGame(gameId);
  const { games } = useGames();
  const { locations, save: saveLocation, isSaving } = useLocations();

  const [openNewLocationModal, setOpenNewLocationModal] = useState(false);

  const { register, handleSubmit, control } = useForm<CreatePlay>({
    resolver: zodResolver(CreatePlaySchema),
    defaultValues: {
      gameId: gameId,
      locationId: '1',
      minutes: game?.maxPlayTime ?? 30,
      comment: '',
      players: [],
    },
  });

  if (locations === undefined || games === undefined) return null;

  const onSubmit = (data: CreatePlay) => {
    console.log(data);
  };

  return (
    <BgtPage>
      <BgtPageContent className="flex flex-col gap-8">
        <div className="flex justify-center">Create game page header (CHANGE ME)</div>
        <form onSubmit={(event) => void handleSubmit(onSubmit)(event)}>
          <div className="flex flex-col gap-3">
            <FormRow
              title={t('playplayer.new.game.label')}
              subTitle={t('playplayer.new.game.sub-label')}
              right={
                <BgtSelectNoLabel<CreatePlay>
                  items={
                    games?.map((x) => ({
                      value: x.id.toString(),
                      label: x.title,
                      image: x.image,
                    })) ?? []
                  }
                  control={control}
                  name="gameId"
                />
              }
            />
            <FormRow
              title={t('playplayer.new.location.label')}
              subTitle={t('playplayer.new.location.sub-label')}
              right={
                <div className="flex flex-row gap-3">
                  <BgtSelectNoLabel
                    items={
                      locations?.map((x) => ({
                        value: x.id.toString(),
                        label: x.name,
                      })) ?? []
                    }
                    control={control}
                    name="locationId"
                  />
                  <Button size="3" onClick={() => setOpenNewLocationModal(true)}>
                    create new
                  </Button>
                </div>
              }
            />
            <FormRow
              title={t('playplayer.new.location.label')}
              subTitle={t('playplayer.new.location.sub-label')}
              right={
                <div className="flex flex-row gap-3">
                  <BgtComboBox
                    options={
                      locations?.map((x) => ({
                        value: x.id.toString(),
                        label: x.name,
                      })) ?? []
                    }
                    placeholder={''}
                    addOptionText={(value) => t('playplayer.new.location.create-new', { name: value })}
                    onChange={(value) => console.log(value)}
                    onCreate={(value) => saveLocation({ name: value })}
                    isSaving={isSaving}
                  />
                </div>
              }
            />
            <FormRow
              title={t('playplayer.new.players.label')}
              subTitle={t('playplayer.new.players.sub-label')}
              right={<BgtPlayerSelector name="players" control={control} hasScoring={game?.hasScoring ?? true} />}
            />
            <FormRow
              title={t('playplayer.new.start.label')}
              subTitle={t('playplayer.new.start.sub-label')}
              right={<BgtInputField name="start" type="date" register={register} className="max-w-64" control={control} />}
            />
            <FormRow
              title={t('playplayer.new.duration.label')}
              subTitle={t('playplayer.new.duration.sub-label')}
              right={<BgtInputField valueAsNumber name="minutes" type="number" register={register} className="max-w-64" control={control} />}
            />
            <FormRow
              title={t('playplayer.new.comment.label')}
              subTitle={t('playplayer.new.comment.sub-label')}
              right={<BgtTextArea name="comment" register={register} disabled={false} className="max-w-80" />}
            />
            <EmptyFormRow right={<Button type="submit">{t('playplayer.save')}</Button>} />
          </div>
        </form>
        <BgtNewLocationModal open={openNewLocationModal} setOpen={setOpenNewLocationModal} />
      </BgtPageContent>
    </BgtPage>
  );
};
