import { z } from 'zod';
import { useTranslation } from 'react-i18next';
import { useMemo } from 'react';
import { useForm } from '@tanstack/react-form';

import { MultiSelectField } from './MultiSelectField';

import { Game, Location, Player } from '@/models';
import { BgtFormField, BgtInputField, BgtSelect, BgtTextArea, BgtDateTimePicker } from '@/components/BgtForm';

export const GameNightFormSchema = z.object({
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

export interface GameNightFormValues {
  title: string;
  startDate: Date;
  locationId: number;
  hostId: number;
  notes: string;
  suggestedGameIds: number[];
  invitedPlayerIds: number[];
}

interface Props {
  defaultValues?: Partial<GameNightFormValues>;
  players: Player[];
  games: Game[];
  locations: Location[];
  isLoading: boolean;
  onSubmit: (values: GameNightFormValues) => Promise<void>;
  children?: React.ReactNode;
  onClose: () => void;
}

export const GameNightForm = (props: Props) => {
  const { defaultValues, players, games, locations, isLoading, onSubmit, children, onClose } = props;
  const { t } = useTranslation();

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
      title: defaultValues?.title ?? '',
      startDate: defaultValues?.startDate ?? new Date(),
      locationId: defaultValues?.locationId ?? 0,
      hostId: defaultValues?.hostId ?? 0,
      notes: defaultValues?.notes ?? '',
      selectedPlayers: defaultValues?.invitedPlayerIds ?? [],
      selectedGames: defaultValues?.suggestedGameIds ?? [],
    },
    onSubmit: async ({ value }) => {
      const validatedData = GameNightFormSchema.parse(value);
      await onSubmit({
        ...validatedData,
        notes: validatedData.notes ?? '',
        suggestedGameIds: value.selectedGames,
        invitedPlayerIds: value.selectedPlayers,
      });
      handleClose();
    },
  });

  const handleClose = () => {
    form.reset();
    onClose();
  };

  return (
    <form
      id="game-night-form"
      onSubmit={(e) => {
        e.preventDefault();
        e.stopPropagation();
        form.handleSubmit();
      }}
      className="w-full"
    >
      {children}

      <div className="flex flex-col gap-4 mt-3 mb-3 max-h-[60vh] overflow-y-auto pr-2">
        <BgtFormField form={form} name="title" schema={GameNightFormSchema}>
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

        <BgtFormField form={form} name="startDate" schema={GameNightFormSchema}>
          {(field) => (
            <BgtDateTimePicker field={field} disabled={isLoading} label={t('game-nights.form.start.label')} />
          )}
        </BgtFormField>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <BgtFormField form={form} name="hostId" schema={GameNightFormSchema}>
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
          <BgtFormField form={form} name="locationId" schema={GameNightFormSchema}>
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

        <form.Field name="selectedPlayers">
          {(field) => (
            <MultiSelectField
              label={t('game-nights.form.invited-players.label')}
              options={playerOptions}
              selected={field.state.value}
              disabled={isLoading}
              onChange={(values) => field.handleChange(values)}
              placeholder={t('game-nights.form.invited-players.description')}
            />
          )}
        </form.Field>

        <form.Field name="selectedGames">
          {(field) => (
            <MultiSelectField
              label={t('game-nights.form.suggested-games.label')}
              options={gameOptions}
              selected={field.state.value}
              disabled={isLoading}
              onChange={(values) => field.handleChange(values)}
              placeholder={t('game-nights.form.suggested-games.description')}
            />
          )}
        </form.Field>

        <BgtFormField form={form} name="notes" schema={GameNightFormSchema}>
          {(field) => <BgtTextArea field={field} label={t('game-nights.form.notes.label')} disabled={isLoading} />}
        </BgtFormField>
      </div>
    </form>
  );
};
