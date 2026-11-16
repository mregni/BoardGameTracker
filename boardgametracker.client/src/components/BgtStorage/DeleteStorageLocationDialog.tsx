import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import {
	BgtDialog,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogFooter,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import { BgtText } from "@/components/BgtText/BgtText";
import { useStorageActions, useStorageData } from "@/hooks/useStorage";
import { StorageLayout, type StorageLocation } from "@/models";
import { getGames } from "@/services/queries/games";
import { descendantIds, itemsIn, nodeName, placeLabel, storageItems } from "@/utils/storage";

interface Props {
	node: StorageLocation;
	close: () => void;
	onDeleted?: () => void;
}

const SHOWN = 12;

export const DeleteStorageLocationDialog = ({ node, close, onDeleted }: Props) => {
	const { t } = useTranslation(["storage", "common"]);
	const { nodes, locations } = useStorageData();
	const { data: games = [] } = useQuery(getGames());
	const { deleteStorageLocation, isPending } = useStorageActions();

	const name = nodeName(node, nodes, t);
	const inside = descendantIds(nodes, node.id).slice(1);
	const slots = inside.filter((id) => nodes.find((x) => x.id === id)?.layout === StorageLayout.Slot).length;
	const items = itemsIn(storageItems(games), { locationId: node.locationId, storageLocationId: node.id }, nodes);
	const target = placeLabel({ locationId: node.locationId, storageLocationId: node.parentId }, nodes, locations, t);

	const confirm = async () => {
		try {
			await deleteStorageLocation(node.id);
			onDeleted?.();
			close();
		} catch {
			// the mutation already shows the error
		}
	};

	return (
		<BgtDialog open onClose={close}>
			<BgtDialogContent>
				<BgtDialogTitle>{t("delete.title", { name })}</BgtDialogTitle>
				{inside.length > 0 && (
					<BgtDialogDescription>
						{slots === inside.length
							? t("delete.slots", { count: slots })
							: t("delete.inside", { count: inside.length })}
					</BgtDialogDescription>
				)}
				{items.length === 0 ? (
					<BgtText color="gray">{t("delete.empty")}</BgtText>
				) : (
					<div className="flex flex-col gap-3">
						<div className="flex flex-col gap-2 rounded-lg border border-card-border bg-card-black p-3">
							<p className="flex items-baseline gap-2">
								<span className="text-xl font-bold tabular-nums text-card-value">{items.length}</span>
								<span>{t("delete.moves", { count: items.length })}</span>
							</p>
							<p className="font-semibold">{target}</p>
							<ul className="flex flex-wrap items-center gap-1" aria-label={t("delete.moves", { count: items.length })}>
								{items.slice(0, SHOWN).map((item) => (
									<li key={item.key}>
										<BgtAvatar image={item.image} title={item.title} />
									</li>
								))}
								{items.length > SHOWN && (
									<li className="text-sm text-white/60">{t("delete.more", { count: items.length - SHOWN })}</li>
								)}
							</ul>
						</div>
						<BgtText size="2" color="gray">
							{t("delete.keep-order")}
						</BgtText>
					</div>
				)}
				<BgtDialogFooter
					onCancel={close}
					cancelLabel={t("common:cancel")}
					confirmLabel={t("delete.confirm", { name })}
					variant="destructive"
					onConfirm={confirm}
					disabled={isPending}
					cancelDisabled={isPending}
				/>
			</BgtDialogContent>
		</BgtDialog>
	);
};
