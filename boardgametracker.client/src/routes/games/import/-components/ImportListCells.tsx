import type { CellContext } from "@tanstack/react-table";
import { cx } from "class-variance-authority";
import { createContext, useContext } from "react";
import { useTranslation } from "react-i18next";
import LinkIcon from "@/assets/icons/arrow-square-out.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtSimpleCheckbox, BgtSimpleInputField, BgtSimpleSelect, BgtSimpleSwitch } from "@/components/BgtForm";
import { useDecimalInput } from "@/hooks/useDecimalInput";
import { GameState, type ImportGame } from "@/models";
import { getItemStateTranslationKey } from "@/utils/ItemStateUtils";
import { parseLocalDate } from "@/utils/localDate";

interface ImportListState {
	selectAll: {
		checked: boolean;
		disabled: boolean;
		onChange: (checked: boolean) => void;
	};
	updateGame: (bggId: number, updates: Partial<ImportGame>) => void;
	currency?: string;
}

export const ImportListContext = createContext<ImportListState | undefined>(undefined);

const useImportList = () => {
	const context = useContext(ImportListContext);
	if (!context) throw new Error("ImportListContext is missing");
	return context;
};

interface ImportPriceInputProps {
	price: number;
	onChange: (price: number) => void;
	placeholder: string;
	disabled: boolean;
	currency?: string;
	ariaLabel: string;
}

const ImportPriceInput = ({ price, onChange, placeholder, disabled, currency, ariaLabel }: ImportPriceInputProps) => {
	const decimal = useDecimalInput(price || null, (value) => onChange(value ?? 0));
	return (
		<BgtSimpleInputField
			type="text"
			inputMode="decimal"
			value={decimal.value}
			onChange={(event) => decimal.onChange(event.target.value)}
			onBlur={decimal.onBlur}
			placeholder={placeholder}
			disabled={disabled}
			className="w-32.5"
			suffixLabel={currency}
			ariaLabel={ariaLabel}
		/>
	);
};

type ImportCellProps = CellContext<ImportGame, unknown>;

export const SelectAllHeader = () => {
	const { selectAll } = useImportList();
	return (
		<BgtSimpleCheckbox
			id="select-all-import"
			label=""
			disabled={selectAll.disabled}
			checked={selectAll.checked}
			onCheckedChange={(checked) => selectAll.onChange(checked)}
		/>
	);
};

export const CheckboxCell = ({ row }: ImportCellProps) => {
	const { updateGame } = useImportList();
	return (
		<BgtSimpleCheckbox
			id={`import-${row.original.bggId}`}
			disabled={row.original.inCollection || row.original.lockedByExpansion}
			label={""}
			aria-label={row.original.title}
			aria-describedby={row.original.lockedByExpansion ? `import-locked-${row.original.bggId}` : undefined}
			checked={row.original.checked}
			onCheckedChange={(state) => {
				updateGame(row.original.bggId, { checked: state });
			}}
		/>
	);
};

export const AvatarCell = ({ row }: ImportCellProps) => (
	<div
		className={cx(
			row.original.baseGameBggId != null && [
				"relative pl-6",
				"before:absolute before:left-2 before:-top-3 before:-bottom-3 before:w-px before:bg-card-border",
				"after:absolute after:left-2 after:top-1/2 after:h-px after:w-3 after:bg-card-border",
			],
		)}
	>
		<BgtAvatar
			title={row.original.title}
			image={row.original.imageUrl}
			size={row.original.baseGameBggId != null ? "medium" : "large"}
			disabled={row.original.inCollection}
		/>
	</div>
);

export const NameCell = ({ row }: ImportCellProps) => {
	const { t } = useTranslation(["common", "game", "bgg-import", "games"]);
	return (
		<div className="flex flex-col gap-1">
			<span className={cx("flex items-center gap-2", row.original.inCollection && "text-gray-400")}>
				{row.original.baseGameBggId != null && (
					<span className="rounded border border-primary/30 px-1 text-[11px] leading-4 tracking-wider text-stat-title">
						{t("bgg-import:expansion-tag")}
					</span>
				)}
				{row.original.title}
			</span>
			{row.original.inCollection && row.original.baseGameBggId == null && (
				<span className="text-xs font-normal text-white/60">{t("bgg-import:in-collection")}</span>
			)}
			{row.original.addedForExpansion && (
				<span
					id={`import-locked-${row.original.bggId}`}
					className="inline-flex w-fit items-center rounded-full border border-warn/50 bg-warn/10 px-2 py-0.5 text-xs font-medium text-warn"
				>
					{t("bgg-import:added-for-expansion")}
				</span>
			)}
			{row.original.lockedByExpansion && !row.original.addedForExpansion && (
				<span id={`import-locked-${row.original.bggId}`} className="text-xs font-normal text-white/60">
					{t("bgg-import:needed-by-expansion")}
				</span>
			)}
		</div>
	);
};

export const BggIdCell = ({ row }: ImportCellProps) => (
	<div>
		<a
			className="underline text-blue-700 cursor-pointer flex flex-row gap-1"
			href={`https://boardgamegeek.com/boardgame/${row.original.bggId}`}
			target="_blank"
			rel="noopener noreferrer"
		>
			{row.original.bggId} <LinkIcon className="size-4" />
		</a>
	</div>
);

export const PriceCell = ({ row }: ImportCellProps) => {
	const { t } = useTranslation(["common", "game", "bgg-import", "games"]);
	const { updateGame, currency } = useImportList();
	return (
		<ImportPriceInput
			price={row.original.price}
			onChange={(price) => updateGame(row.original.bggId, { price })}
			placeholder={t("game:price.placeholder")}
			disabled={row.original.inCollection}
			currency={currency}
			ariaLabel={`${t("game:price.label")}: ${row.original.title}`}
		/>
	);
};

export const StateCell = ({ row }: ImportCellProps) => {
	const { t } = useTranslation(["common", "game", "bgg-import", "games"]);
	const { updateGame } = useImportList();
	return (
		<BgtSimpleSelect
			value={row.original.state}
			onValueChange={(value) => updateGame(row.original.bggId, { state: value as GameState })}
			disabled={row.original.inCollection}
			aria-label={`${t("state")}: ${row.original.title}`}
			items={Object.values(GameState).map((value) => ({
				label: t(getItemStateTranslationKey(value, false)),
				value: value,
			}))}
		/>
	);
};

export const ScoringCell = ({ row }: ImportCellProps) => {
	const { t } = useTranslation(["common", "game", "bgg-import", "games"]);
	const { updateGame } = useImportList();
	return row.original.baseGameBggId != null ? (
		<span className="text-sm font-normal text-white/60">{t("bgg-import:scoring-from-base")}</span>
	) : (
		<BgtSimpleSwitch
			label={t("game:scoring.label")}
			value={row.original.hasScoring}
			disabled={row.original.inCollection}
			onChange={(value) => updateGame(row.original.bggId, { hasScoring: value })}
		/>
	);
};

export const AddedDateCell = ({ row }: ImportCellProps) => {
	const { t } = useTranslation(["common", "game", "bgg-import", "games"]);
	const { updateGame } = useImportList();
	return (
		<BgtSimpleInputField
			value={row.original.addedDate}
			onChange={(event) =>
				updateGame(row.original.bggId, {
					addedDate: parseLocalDate(event.target.value) ?? row.original.addedDate,
				})
			}
			type="date"
			disabled={row.original.inCollection}
			ariaLabel={`${t("game:added-date.label")}: ${row.original.title}`}
		/>
	);
};
