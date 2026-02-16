import { useTranslation } from 'react-i18next';
import type { AnyFieldApi } from '@tanstack/react-form';

import { gameFormOpts } from '../-utils/gameFormOpts';

import { zodValidator } from '@/utils/zodValidator';
import { getItemStateTranslationKey } from '@/utils/ItemStateUtils';
import { CreateGameSchema, GameState } from '@/models';
import { withForm } from '@/hooks/form';
import { BgtDatePicker, BgtInputField, BgtSelect, BgtTextArea } from '@/components/BgtForm';

export const GameFormPlayerFields = withForm({
  ...gameFormOpts,
  props: {
    disabled: false,
    currency: undefined as string | undefined,
  },
  render: function Render({ form, disabled, currency }) {
    const { t } = useTranslation();

    return (
      <>
        <form.Field name="bggId" validators={zodValidator(CreateGameSchema, 'bggId')}>
          {(field: AnyFieldApi) => (
            <BgtInputField field={field} type="number" disabled={disabled} label={t('game.bgg.placeholder')} />
          )}
        </form.Field>
        <form.Field name="buyingPrice" validators={zodValidator(CreateGameSchema, 'buyingPrice')}>
          {(field: AnyFieldApi) => (
            <BgtInputField
              field={field}
              label={t('game.price.label')}
              type="number"
              placeholder={t('game.price.placeholder')}
              disabled={disabled}
              prefixLabel={currency}
            />
          )}
        </form.Field>
        <form.Field name="additionDate" validators={zodValidator(CreateGameSchema, 'additionDate')}>
          {(field: AnyFieldApi) => (
            <BgtDatePicker
              field={field}
              label={t('game.added-date.label')}
              disabled={disabled}
              placeholder={t('game.added-date.placeholder')}
            />
          )}
        </form.Field>
        <form.Field name="state" validators={zodValidator(CreateGameSchema, 'state')}>
          {(field: AnyFieldApi) => (
            <BgtSelect
              field={field}
              label={t('game.state.label')}
              disabled={disabled}
              items={Object.values(GameState).map((value) => ({
                label: t(getItemStateTranslationKey(value, false)),
                value: value,
              }))}
            />
          )}
        </form.Field>
        <form.Field name="yearPublished" validators={zodValidator(CreateGameSchema, 'yearPublished')}>
          {(field: AnyFieldApi) => (
            <BgtInputField
              field={field}
              label={t('game.new.manual.year.label')}
              type="number"
              disabled={disabled}
              className="pr-2"
            />
          )}
        </form.Field>
        <form.Field name="description" validators={zodValidator(CreateGameSchema, 'description')}>
          {(field: AnyFieldApi) => (
            <BgtTextArea field={field} label={t('game.new.manual.description.label')} disabled={disabled} />
          )}
        </form.Field>
        <div className="flex flex-row gap-2">
          <form.Field name="minPlayers" validators={zodValidator(CreateGameSchema, 'minPlayers')}>
            {(field: AnyFieldApi) => (
              <BgtInputField
                field={field}
                label={t('game.new.manual.min-players.label')}
                type="number"
                disabled={disabled}
                className="pr-2"
              />
            )}
          </form.Field>
          <form.Field name="maxPlayers" validators={zodValidator(CreateGameSchema, 'maxPlayers')}>
            {(field: AnyFieldApi) => (
              <BgtInputField
                field={field}
                label={t('game.new.manual.max-players.label')}
                type="number"
                disabled={disabled}
                className="pr-2"
              />
            )}
          </form.Field>
        </div>
      </>
    );
  },
});
