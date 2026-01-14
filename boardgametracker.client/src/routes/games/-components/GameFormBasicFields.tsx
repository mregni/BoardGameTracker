import { useTranslation } from 'react-i18next';
import { memo } from 'react';
import { UseFormReturn } from '@tanstack/react-form';

import { getItemStateTranslationKeyByString } from '@/utils/ItemStateUtils';
import { CreateGame, CreateGameSchema, GameState } from '@/models';
import { BgtFormField, BgtInputField, BgtSelect, BgtDatePicker, BgtTextArea } from '@/components/BgtForm';

interface GameFormBasicFieldsProps {
  form: UseFormReturn<CreateGame>;
  disabled: boolean;
  currency?: string;
}

const GameFormBasicFieldsComponent = ({ form, disabled, currency }: GameFormBasicFieldsProps) => {
  const { t } = useTranslation();

  return (
    <>
      <BgtFormField form={form} name="title" schema={CreateGameSchema.shape.title}>
        {(field) => (
          <BgtInputField field={field} type="text" disabled={disabled} label={t('game.new.manual.game-title.label')} />
        )}
      </BgtFormField>
      <BgtFormField form={form} name="bggId" schema={CreateGameSchema.shape.bggId}>
        {(field) => <BgtInputField field={field} type="number" disabled={disabled} label={t('game.bgg.placeholder')} />}
      </BgtFormField>
      <BgtFormField form={form} name="buyingPrice" schema={CreateGameSchema.shape.buyingPrice}>
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
      <BgtFormField form={form} name="additionDate" schema={CreateGameSchema.shape.additionDate}>
        {(field) => (
          <BgtDatePicker
            field={field}
            label={t('game.added-date.label')}
            disabled={disabled}
            placeholder={t('game.added-date.placeholder')}
          />
        )}
      </BgtFormField>
      <BgtFormField form={form} name="state" schema={CreateGameSchema.shape.state}>
        {(field) => (
          <BgtSelect
            field={field}
            label={t('game.state.label')}
            disabled={disabled}
            items={Object.keys(GameState)
              .filter((value) => !isNaN(Number(value)))
              .map((value) => ({ label: t(getItemStateTranslationKeyByString(value)), value: value }))}
          />
        )}
      </BgtFormField>
      <BgtFormField form={form} name="yearPublished" schema={CreateGameSchema.shape.yearPublished}>
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
      <BgtFormField form={form} name="description" schema={CreateGameSchema.shape.description}>
        {(field) => <BgtTextArea field={field} label={t('game.new.manual.description.label')} disabled={disabled} />}
      </BgtFormField>
    </>
  );
};

GameFormBasicFieldsComponent.displayName = 'GameFormBasicFields';

export const GameFormBasicFields = memo(GameFormBasicFieldsComponent);
