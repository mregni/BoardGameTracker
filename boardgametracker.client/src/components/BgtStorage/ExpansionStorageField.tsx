import { noop } from "@tanstack/react-query";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import { BgtSimpleSwitch } from "@/components/BgtForm";
import { BgtText } from "@/components/BgtText/BgtText";
import { useStorageActions, useStorageData } from "@/hooks/useStorage";
import { type Expansion, StorageItemType } from "@/models";
import { canStore, placeLabel, placeOf, type StoragePlace } from "@/utils/storage";
import { StoragePicker } from "./StoragePicker";

interface Props {
	expansion: Expansion;
	baseTitle: string;
	canWrite: boolean;
}

export const ExpansionStorageField = ({ expansion, baseTitle, canWrite }: Props) => {
	const { t } = useTranslation(["game", "storage"]);
	const { nodes, locations } = useStorageData();
	const { assignStorage, isPending } = useStorageActions();
	const [picking, setPicking] = useState(false);
	const place = placeOf(expansion);

	if (!canStore(expansion.state)) {
		return (
			<BgtText size="2" color="gray">
				{t("storage.only-owned")}
			</BgtText>
		);
	}

	if (!canWrite) {
		return (
			<BgtText size="2">
				{place ? placeLabel(place, nodes, locations, t) : t("storage:place.with-base", { title: baseTitle })}
			</BgtText>
		);
	}

	const assign = (target: StoragePlace | null) =>
		assignStorage({
			items: [{ type: StorageItemType.Expansion, id: expansion.id }],
			locationId: target?.locationId ?? null,
			storageLocationId: target?.storageLocationId ?? null,
		}).catch(noop);

	const inBox = place === null && !picking;
	const name = `${t("storage:picker.label")}: ${expansion.title}`;

	return (
		<fieldset className="flex min-w-0 flex-col gap-2 sm:flex-row sm:items-center sm:gap-4">
			<legend className="sr-only">{name}</legend>
			<BgtSimpleSwitch
				label={t("storage.in-base-box", { title: baseTitle })}
				value={inBox}
				disabled={isPending}
				onChange={(on) => {
					setPicking(!on);
					if (on && place) void assign(null);
				}}
			/>
			{!inBox && (
				<StoragePicker
					value={place}
					onChange={(target) => {
						if (target) void assign(target);
					}}
					canCreate
					disabled={isPending}
					aria-label={name}
					className="min-w-0 sm:flex-1"
				/>
			)}
		</fieldset>
	);
};
