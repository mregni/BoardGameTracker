import { z } from 'zod';
import { Bars } from 'react-loading-icons';
import { useTranslation } from 'react-i18next';
import { useMemo, useState } from 'react';
import { useForm } from '@tanstack/react-form';

import { MultiSelectField } from '../-components/MultiSelectField';

import { CreateGameNight, Game, Location, Player } from '@/models';
import { BgtFormField, BgtInputField, BgtSelect, BgtTextArea, BgtDateTimePicker } from '@/components/BgtForm';
import {
  BgtDialog,
  BgtDialogContent,
  BgtDialogDescription,
  BgtDialogClose,
  BgtDialogTitle,
} from '@/components/BgtDialog';
import BgtButton from '@/components/BgtButton/BgtButton';

const CreateGameNightSchema = z.object({
  title: z.string().min(1, { message: 'game-nights.validation.title-required' }),
  startDate: z.coerce.date({
    errorMap: () => {
      return { message: 'player-session.new.start.required' };
    },
  }),
  locationId: z.number().min(1, { message: 'game-nights.validation.location-required' }),
  hostId: z.number().min(1, { message: 'game-nights.validation.host-required' }),
  notes: z.string().optional(),
});

interface Props {
  open: boolean;
  setOpen: React.Dispatch<React.SetStateAction<boolean>>;
  players: Player[];
  games: Game[];
  locations: Location[];
  isLoading: boolean;
  onSave: (gameNight: CreateGameNight) => Promise<void>;
}

export const CreateGameNightModal = (props: Props) => {
  const { open, setOpen, players, games, locations, isLoading, onSave } = props;
  const { t } = useTranslation();

  const [selectedPlayers, setSelectedPlayers] = useState<number[]>([]);
  const [selectedGames, setSelectedGames] = useState<number[]>([]);

  const playerOptions = useMemo(
    () =>
      players.map((p) => ({
        value: p.id,
        label: p.name,
        image: p.image,
      })),
    [players]
  );

  const gameOptions = useMemo(
    () =>
      games.map((g) => ({
        value: g.id,
        label: g.title,
        image: g.image,
      })),
    [games]
  );

  const locationOptions = useMemo(
    () =>
      locations.map((l) => ({
        value: l.id,
        label: l.name,
      })),
    [locations]
  );

  const hostOptions = useMemo(
    () =>
      players.map((p) => ({
        value: p.id,
        label: p.name,
        image: p.image,
      })),
    [players]
  );

  const form = useForm({
    defaultValues: {
      title: '',
      startDate: new Date(),
      locationId: 0,
      hostId: 0,
      notes: '',
    },
    onSubmit: async ({ value }) => {
      const validatedData = CreateGameNightSchema.parse(value);
      const gameNight: CreateGameNight = {
        ...validatedData,
        notes: validatedData.notes ?? '',
        suggestedGameIds: selectedGames,
        invitedPlayerIds: selectedPlayers,
      };
      await onSave(gameNight);
      handleClose();
    },
  });

  const handleClose = () => {
    form.reset();
    setSelectedPlayers([]);
    setSelectedGames([]);
    setOpen(false);
  };

  return (
    <BgtDialog open={open} onClose={handleClose}>
      <BgtDialogContent className="max-w-2xl!">
        <form
          onSubmit={(e) => {
            e.preventDefault();
            e.stopPropagation();
            form.handleSubmit();
          }}
          className="w-full"
        >
          <BgtDialogTitle>{t('game-nights.create.title')}</BgtDialogTitle>
          <BgtDialogDescription>{t('game-nights.create.description')}</BgtDialogDescription>
          <div className="flex flex-col gap-4 mt-3 mb-3 max-h-[60vh] overflow-y-auto pr-2">
            <BgtFormField form={form} name="title" schema={CreateGameNightSchema.shape.title}>
              {(field) => (
                <BgtInputField
                  field={field}
                  type="text"
                  label={t('game-nights.form.title.label')}
                  disabled={isLoading}
                  placeholder={t('game-nights.form.title.placeholder')}
                />
              )}
            </BgtFormField>

            <BgtFormField form={form} name="startDate" schema={CreateGameNightSchema.shape.startDate}>
              {(field) => (
                <BgtDateTimePicker field={field} disabled={isLoading} label={t('game-nights.form.start.label')} />
              )}
            </BgtFormField>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <BgtFormField form={form} name="hostId" schema={CreateGameNightSchema.shape.hostId}>
                {(field) => (
                  <BgtSelect
                    field={field}
                    hasSearch
                    items={hostOptions}
                    label={t('game-nights.form.host.label')}
                    disabled={isLoading}
                    placeholder={t('game-nights.form.host.placeholder')}
                  />
                )}
              </BgtFormField>
              <BgtFormField form={form} name="locationId" schema={CreateGameNightSchema.shape.locationId}>
                {(field) => (
                  <BgtSelect
                    field={field}
                    hasSearch
                    items={locationOptions}
                    label={t('game-nights.form.location.label')}
                    disabled={isLoading}
                    placeholder={t('game-nights.form.location.placeholder')}
                  />
                )}
              </BgtFormField>
            </div>

            <MultiSelectField
              label={t('game-nights.form.invited-players.label')}
              options={playerOptions}
              selected={selectedPlayers}
              disabled={isLoading}
              onChange={setSelectedPlayers}
              placeholder={t('game-nights.form.invited-players.description')}
            />

            <MultiSelectField
              label={t('game-nights.form.suggested-games.label')}
              options={gameOptions}
              selected={selectedGames}
              disabled={isLoading}
              onChange={setSelectedGames}
              placeholder={t('game-nights.form.suggested-games.description')}
            />

            <BgtFormField form={form} name="notes" schema={CreateGameNightSchema.shape.notes}>
              {(field) => <BgtTextArea field={field} label={t('game-nights.form.notes.label')} disabled={isLoading} />}
            </BgtFormField>
          </div>
          <BgtDialogClose>
            <BgtButton disabled={isLoading} variant="cancel" className="flex-1" onClick={handleClose}>
              {t('common.cancel')}
            </BgtButton>
            <BgtButton type="submit" disabled={isLoading} className="flex-1" variant="primary">
              {isLoading && <Bars className="size-4" />}
              {t('game-nights.create.save')}
            </BgtButton>
          </BgtDialogClose>
        </form>
      </BgtDialogContent>
    </BgtDialog>
  );
};
