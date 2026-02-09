import { useTranslation } from 'react-i18next';
import { memo } from 'react';

import { getItemStateTranslationKey } from '@/utils/ItemStateUtils';
import { CreateGameSchema, GameState } from '@/models';
import { BgtDatePicker, BgtFormField, BgtInputField, BgtSelect, BgtTextArea } from '@/components/BgtForm';

interface GameFormPlayerFieldsProps {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  form: any;
  disabled: boolean;
  currency?: string;
}

const GameFormPlayerFieldsComponent = ({ form, disabled, currency }: GameFormPlayerFieldsProps) => {
  const { t } = useTranslation();

  return (
    <>
      <BgtFormField form={form} name="bggId" schema={CreateGameSchema}>
        {(field) => <BgtInputField field={field} type="number" disabled={disabled} label={t('game.bgg.placeholder')} />}
      </BgtFormField>
      <BgtFormField form={form} name="buyingPrice" schema={CreateGameSchema}>
        {(field) => (
          <BgtInputField
            field={field}
            label={t('game.price.label')}
            type="number"
            placeholder={t('game.price.placeholder')}
            disabled={disabled}
            prefixLabel={currency}
          />
        )}
      </BgtFormField>
      <BgtFormField form={form} name="additionDate" schema={CreateGameSchema}>
        {(field) => (
          <BgtDatePicker
            field={field}
            label={t('game.added-date.label')}
            disabled={disabled}
            placeholder={t('game.added-date.placeholder')}
          />
        )}
      </BgtFormField>
      <BgtFormField form={form} name="state" schema={CreateGameSchema}>
        {(field) => (
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
      </BgtFormField>
      <BgtFormField form={form} name="yearPublished" schema={CreateGameSchema}>
        {(field) => (
          <BgtInputField
            field={field}
            label={t('game.new.manual.year.label')}
            type="number"
            disabled={disabled}
            className="pr-2"
          />
        )}
      </BgtFormField>
      <BgtFormField form={form} name="description" schema={CreateGameSchema}>
        {(field) => <BgtTextArea field={field} label={t('game.new.manual.description.label')} disabled={disabled} />}
      </BgtFormField>
      <div className="flex flex-row gap-2">
        <BgtFormField form={form} name="minPlayers" schema={CreateGameSchema}>
          {(field) => (
            <BgtInputField
              field={field}
              label={t('game.new.manual.min-players.label')}
              type="number"
              disabled={disabled}
              className="pr-2"
            />
          )}
        </BgtFormField>
        <BgtFormField form={form} name="maxPlayers" schema={CreateGameSchema}>
          {(field) => (
            <BgtInputField
              field={field}
              label={t('game.new.manual.max-players.label')}
              type="number"
              disabled={disabled}
              className="pr-2"
            />
          )}
        </BgtFormField>
      </div>
    </>
  );
};

GameFormPlayerFieldsComponent.displayName = 'GameFormPlayerFields';

export const GameFormPlayerFields = memo(GameFormPlayerFieldsComponent);
