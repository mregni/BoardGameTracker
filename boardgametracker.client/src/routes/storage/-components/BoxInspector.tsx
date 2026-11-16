import { Link } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import PackageIcon from "@/assets/icons/package.svg?react";
import XIcon from "@/assets/icons/x.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtFieldLabel } from "@/components/BgtForm";
import { BgtIconButton } from "@/components/BgtIconButton/BgtIconButton";
import { BgtSegmentedControl } from "@/components/BgtSegmentedControl/BgtSegmentedControl";
import { StoragePicker } from "@/components/BgtStorage/StoragePicker";
import { BgtText } from "@/components/BgtText/BgtText";
import { BoxOrientation, type Location, type StorageLocation } from "@/models";
import { getItemStateTranslationKey } from "@/utils/ItemStateUtils";
import { placeLabel, placeOf, type StorageItem, type StoragePlace } from "@/utils/storage";
import { boxLabel } from "./StorageBox";

interface Props {
	item: StorageItem;
	nodes: StorageLocation[];
	locations: Location[];
	canWrite: boolean;
	canShift: { earlier: boolean; later: boolean };
	onClose: () => void;
	onOrientation: (orientation: BoxOrientation) => void;
	onShift: (direction: -1 | 1) => void;
	onMove: (place: StoragePlace) => void;
}

export const BoxInspector = (props: Props) => {
	const { item, nodes, locations, canWrite, canShift, onClose, onOrientation, onShift, onMove } = props;
	const { t } = useTranslation(["storage", "common"]);
	const state = item.expansion?.state ?? item.game.state;
	const inBox = item.expansion ? [] : item.game.expansions.filter((expansion) => placeOf(expansion) === null);
	const elsewhere = item.expansion ? 0 : item.game.expansions.length - inBox.length;

	return (
		<BgtCard aria-labelledby="box-inspector-title">
			<div className="flex flex-col gap-4">
				<div className="flex items-center justify-between gap-2">
					<h2 id="box-inspector-title" className="flex items-center gap-2 tracking-wide">
						<PackageIcon className="size-5 text-primary" aria-hidden="true" />
						{t("box.title")}
					</h2>
					<BgtIconButton
						icon={<XIcon className="size-4" />}
						intent="subtile"
						onClick={onClose}
						aria-label={t("box.close")}
					/>
				</div>
				<div className="flex items-start gap-3">
					<BgtAvatar image={item.image} title={item.title} size="large" />
					<div className="min-w-0">
						<BgtText weight="bold" className="break-words">
							{item.title}
						</BgtText>
						{item.expansion && (
							<BgtText size="1" color="gray">
								{t("box.expansion-of", { title: item.game.title })}
							</BgtText>
						)}
						<BgtText size="2" color="gray">
							{t(getItemStateTranslationKey(state, false))}
						</BgtText>
					</div>
				</div>
				{item.isLoaned && (
					<p className="w-fit rounded-full border border-warn/40 px-2 py-0.5 text-xs text-warn">{t("box.loaned")}</p>
				)}
				<dl className="grid grid-cols-[auto_minmax(0,1fr)] gap-x-3 gap-y-1 text-sm">
					<dt className="text-white/60">{t("box.stored-in")}</dt>
					<dd>{placeLabel(item.place, nodes, locations, t)}</dd>
					{item.game.expansions.length > 0 && !item.expansion && (
						<>
							<dt className="text-white/60">{t("box.expansions")}</dt>
							<dd>
								{[
									t("box.in-box", { count: inBox.length }),
									elsewhere > 0 ? t("box.elsewhere", { count: elsewhere }) : null,
								]
									.filter(Boolean)
									.join(", ")}
							</dd>
						</>
					)}
				</dl>
				{canWrite && (
					<>
						<div className="flex flex-col gap-1">
							<BgtFieldLabel>{t("orientation.label")}</BgtFieldLabel>
							<BgtSegmentedControl
								aria-label={t("orientation.label")}
								size="sm"
								value={item.orientation}
								onChange={onOrientation}
								options={[BoxOrientation.Front, BoxOrientation.Side, BoxOrientation.Flat].map((value) => ({
									value,
									label: t(`orientation.${value}`),
								}))}
							/>
						</div>
						{(canShift.earlier || canShift.later) && (
							<div className="flex gap-2">
								<button
									type="button"
									disabled={!canShift.earlier}
									onClick={() => onShift(-1)}
									className="rounded-md border border-card-border px-2 py-1 text-xs disabled:opacity-40"
								>
									{t("box.earlier")}
								</button>
								<button
									type="button"
									disabled={!canShift.later}
									onClick={() => onShift(1)}
									className="rounded-md border border-card-border px-2 py-1 text-xs disabled:opacity-40"
								>
									{t("box.later")}
								</button>
							</div>
						)}
						<StoragePicker
							label={t("box.move")}
							aria-label={t("box.move-item", { title: boxLabel(item, t) })}
							value={item.place}
							onChange={(place) => place && onMove(place)}
							canCreate
						/>
					</>
				)}
				<Link
					to="/games/$gameId"
					params={{ gameId: item.game.id }}
					className="text-sm text-primary-text underline-offset-4 hover:underline"
				>
					{t("box.open", { title: item.game.title })}
				</Link>
			</div>
		</BgtCard>
	);
};
