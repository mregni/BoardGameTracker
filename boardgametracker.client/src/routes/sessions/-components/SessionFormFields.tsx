import { useTranslation } from 'react-i18next';
import { memo, useMemo } from 'react';

import { CreateSessionSchema, Game, Location } from '@/models';
import { type AnyReactForm, BgtFormField, BgtTextArea, BgtSelect, BgtInputField, BgtDateTimePicker } from '@/components/BgtForm';

interface SessionFormFieldsProps {
  form: AnyReactForm;
  games: Game[];
  locations: Location[];
  disabled: boolean;
}

const SessionFormFieldsComponent = ({ form, games, locations, disabled }: SessionFormFieldsProps) => {
  const { t } = useTranslation();

  const gamesSelectItems = useMemo(
    () =>
      games?.map((x) => ({
        value: x.id,
        label: x.title,
        image: x.image,
      })) ?? [],
    [games]
  );

  const locationsSelectItems = useMemo(
    () =>
      locations?.map((x) => ({
        value: x.id,
        label: x.name,
      })) ?? [],
    [locations]
  );

  return (
    <>
      <BgtFormField form={form} name="gameId" schema={CreateSessionSchema}>
        {(field) => (
          <BgtSelect
            field={field}
            hasSearch
            items={gamesSelectItems}
            label={t('player-session.new.game.label')}
            disabled={disabled}
            placeholder={t('player-session.new.game.placeholder')}
          />
        )}
      </BgtFormField>
      <BgtFormField form={form} name="locationId" schema={CreateSessionSchema}>
        {(field) => (
          <BgtSelect
            field={field}
            hasSearch
            items={locationsSelectItems}
            label={t('player-session.new.location.label')}
            disabled={disabled}
            placeholder={t('player-session.new.location.placeholder')}
          />
        )}
      </BgtFormField>
      <BgtFormField form={form} name="minutes" schema={CreateSessionSchema}>
        {(field) => (
          <BgtInputField
            field={field}
            type="number"
            disabled={disabled}
            label={t('player-session.new.duration.label')}
            placeholder={t('player-session.new.duration.placeholder')}
          />
        )}
      </BgtFormField>
      <BgtFormField form={form} name="start" schema={CreateSessionSchema}>
        {(field) => <BgtDateTimePicker field={field} disabled={disabled} label={t('player-session.new.start.label')} />}
      </BgtFormField>
      <BgtFormField form={form} name="comment" schema={CreateSessionSchema}>
        {(field) => <BgtTextArea field={field} disabled={disabled} label={t('player-session.new.comment.label')} />}
      </BgtFormField>
    </>
  );
};

SessionFormFieldsComponent.displayName = 'SessionFormFields';

export const SessionFormFields = memo(SessionFormFieldsComponent);
